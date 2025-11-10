
using System.IO;
using UnityEngine;

public static class ArtAssetDefines
{
    public static readonly string ApplicationVersion = "4.3.8";

    public static readonly string WorkingDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../UberStrike.ArtAssets"));
    public static readonly string MapAssetBundlePath = Path.GetFullPath(WorkingDirectory + "/AssetBundles/Maps/");
    public static readonly string MapPackagePath = Path.GetFullPath(WorkingDirectory + "/Packages/Maps/");

    public static readonly string MapDeploymentPath = @"\\CMUNEWEB\Websites\client.dev.uberstrike.com\Maps\" + ApplicationVersion;
    public static readonly string RepositoryUrl = "http://kiln.cmune.net/kiln/Code/Cmune/Client/art-assets";

    public static readonly string InstrumentationWebServicesUrl = "http://ws.instrumentation.dev.uberstrike.com/";
    public static readonly string UpdateMapVersionUrl = InstrumentationWebServicesUrl + UberStrike.UnitySdk.Api.Version + "/Cmune.svc/Application/UpdateMapVersion";
}