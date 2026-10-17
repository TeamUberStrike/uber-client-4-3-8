#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Managed-only port of the patched 4.7.1 native scanner. Replaces what
/// <c>uberbeat.dll</c> exports as <c>_HWID@4</c>, <c>_UBERBEAT@4</c> and <c>_SIGNATURE@4</c>.
/// Unity 2022 is x64-only so the original x86 native plugin can never be loaded — everything
/// is reimplemented here using <see cref="Process"/>, <see cref="NetworkInterface"/>,
/// <see cref="X509Certificate"/> and EnumWindows via direct P/Invoke to user32.
/// WMI (BIOS / motherboard / disk) is accessed by reflection on <c>System.Management</c>;
/// the API surface lives in a separate assembly that is not part of .NET Standard 2.1, so
/// queries fall back to <c>UNKNOWN</c> when the assembly isn't present.
/// </summary>
internal static class UberBeatScanner
{
    // ----- Window enumeration -----
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLengthW(IntPtr hWnd);

    public static HashSet<string> EnumerateVisibleWindowTitles()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            EnumWindows((h, _) =>
            {
                if (!IsWindowVisible(h)) return true;
                int len = GetWindowTextLengthW(h);
                if (len <= 0) return true;
                var sb = new StringBuilder(len + 1);
                GetWindowTextW(h, sb, sb.Capacity);
                var title = sb.ToString();
                if (!string.IsNullOrEmpty(title)) set.Add(title);
                return true;
            }, IntPtr.Zero);
        }
        catch (Exception ex) { Debug.LogWarning("[UberBeat] EnumWindows failed: " + ex.Message); }
        return set;
    }

    // ----- Own-process module enumeration -----
    public static HashSet<string> EnumerateOwnModulePaths()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using (var self = Process.GetCurrentProcess())
            {
                foreach (ProcessModule m in self.Modules)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(m.FileName)) set.Add(m.FileName);
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex) { Debug.LogWarning("[UberBeat] own modules failed: " + ex.Message); }
        return set;
    }

    // ----- System process enumeration -----
    public static void EnumerateProcesses(out HashSet<string> processNames, out HashSet<string> processPaths)
    {
        processNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        processPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Process[] procs;
        try { procs = Process.GetProcesses(); }
        catch (Exception ex) { Debug.LogWarning("[UberBeat] GetProcesses failed: " + ex.Message); return; }
        foreach (var p in procs)
        {
            try { processNames.Add(p.ProcessName + ".exe"); } catch { }
            // MainModule.FileName denies access for most non-elevated processes — skip silently.
            try { var path = p.MainModule?.FileName; if (!string.IsNullOrEmpty(path)) processPaths.Add(path); } catch { }
            p.Dispose();
        }
    }

    // ----- Authenticode signer extraction (used for TRUSTED:/SIGNATURE round-trip) -----
    public static string GetSigner(string filePath)
    {
        try
        {
            using (var cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(filePath)))
            {
                return cert.GetNameInfo(X509NameType.SimpleName, forIssuer: false) ?? string.Empty;
            }
        }
        catch { return string.Empty; }
    }

    public static bool IsSigned(string filePath)
    {
        return !string.IsNullOrEmpty(GetSigner(filePath));
    }

    // ----- HWID -----
    public static string BuildHwid()
    {
        string bios = WmiSerial("SELECT * FROM Win32_BIOS", "SerialNumber");
        string board = WmiSerial("SELECT * FROM Win32_BaseBoard", "SerialNumber");
        string hdd = WmiSerial("SELECT SerialNumber FROM Win32_PhysicalMedia", "SerialNumber");
        string mac = GetFirstPhysicalMac();
        return $"BIOS:{bios}|MOTHERBOARD:{board}|HDD:{hdd}|MAC:{mac}";
    }

    private static string GetFirstPhysicalMac()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                byte[] bytes = nic.GetPhysicalAddress().GetAddressBytes();
                if (bytes != null && bytes.Length == 6)
                {
                    return string.Format("{0:X2}:{1:X2}:{2:X2}:{3:X2}:{4:X2}:{5:X2}",
                        bytes[0], bytes[1], bytes[2], bytes[3], bytes[4], bytes[5]);
                }
            }
        }
        catch { }
        return "0.0.0.0";
    }

    private static string WmiSerial(string wql, string property)
    {
        try
        {
            Type searcherType = Type.GetType(
                "System.Management.ManagementObjectSearcher, System.Management, " +
                "Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a92");
            if (searcherType == null) return "UNKNOWN";

            object searcher = Activator.CreateInstance(searcherType, new object[] { wql });
            try
            {
                MethodInfo getMethod = searcherType.GetMethod("Get", Type.EmptyTypes);
                IEnumerable results = getMethod?.Invoke(searcher, null) as IEnumerable;
                if (results == null) return "UNKNOWN";

                foreach (object mgmtObj in results)
                {
                    try
                    {
                        PropertyInfo indexer = mgmtObj.GetType().GetProperty("Item", new[] { typeof(string) });
                        object value = indexer?.GetValue(mgmtObj, new object[] { property });
                        string str = value?.ToString();
                        if (!string.IsNullOrEmpty(str)) return str.Trim();
                    }
                    finally
                    {
                        (mgmtObj as IDisposable)?.Dispose();
                    }
                }
            }
            finally { (searcher as IDisposable)?.Dispose(); }
        }
        catch { }
        return "UNKNOWN";
    }

    // ----- Report assembly -----
    public static string BuildReport()
    {
        var modules = EnumerateOwnModulePaths();
        EnumerateProcesses(out var processNames, out var processPaths);
        var windows = EnumerateVisibleWindowTitles();

        var sb = new StringBuilder(8 * 1024);
        AppendSection(sb, "M:", modules);
        AppendSection(sb, "P:", processNames);
        AppendSection(sb, "PP:", processPaths);
        AppendSection(sb, "W:", windows);
        if (sb.Length > 0 && sb[sb.Length - 1] == '|') sb.Length -= 1;
        return sb.ToString();
    }

    private static void AppendSection(StringBuilder sb, string prefix, HashSet<string> values)
    {
        foreach (var v in values)
        {
            if (string.IsNullOrEmpty(v)) continue;
            // '|' is our outer delimiter; nothing observed in the field has embedded pipes,
            // but strip just in case to keep the server-side split clean.
            sb.Append(prefix).Append(v.Replace('|', ' ')).Append('|');
        }
    }
}
#endif
