#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;

/// <summary>
/// Unity regenerates Info.plist on every iOS export, which drops manually added
/// keys. Content (item icons, weekly special images, maps, audio) is served over
/// plain http:// (client-dev.uberforever.eu has no matching TLS certificate yet),
/// so the release build needs the App Transport Security exception in place.
/// </summary>
public class IosBuildPostProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.iOS)
        {
            return;
        }

        string plistPath = Path.Combine(report.summary.outputPath, "Info.plist");
        if (!File.Exists(plistPath))
        {
            UnityEngine.Debug.LogError("[IosBuildPostProcessor] Info.plist not found at " + plistPath);
            return;
        }

        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict ats = plist.root.CreateDict("NSAppTransportSecurity");
        ats.SetBoolean("NSAllowsArbitraryLoads", true);

        plist.WriteToFile(plistPath);
        UnityEngine.Debug.Log("[IosBuildPostProcessor] Enabled NSAllowsArbitraryLoads in Info.plist");
    }
}
#endif
