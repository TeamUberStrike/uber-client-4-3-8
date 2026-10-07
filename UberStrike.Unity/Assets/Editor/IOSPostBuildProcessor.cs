#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

// Runs automatically after every iOS build (including batch mode).
// Adds NSAllowsArbitraryLoads so the game can reach the UberForever
// web service over plain HTTP (the server uses HTTPS, but the content
// CDN and some internal calls are still HTTP).
public static class IOSPostBuildProcessor
{
    [PostProcessBuild(1)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS)
            return;

        string plistPath = Path.Combine(path, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));

        PlistElementDict ats = plist.root.CreateDict("NSAppTransportSecurity");
        ats.SetBoolean("NSAllowsArbitraryLoads", true);

        File.WriteAllText(plistPath, plist.WriteToString());
        UnityEngine.Debug.Log("[IOSPostBuildProcessor] NSAllowsArbitraryLoads set in Info.plist");
    }
}
#endif
