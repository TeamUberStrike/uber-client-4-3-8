using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Cmune.DataCenter.Common.Entities;
using UnityEditor;
using UnityEngine;

public class SceneExporter
{
    public const string BaseContentUrlProduction = "http://client.uberforever.eu/";
    public const string BaseContentUrlExternalQA = "http://client-qa.uberforever.eu/";
    public const string BaseContentUrlInternalDev = "http://client-dev.uberforever.eu/";

    public const string BaseWebServiceUrlProduction = "http://ws.uberforever.eu:5000/";
    public const string BaseWebServiceUrlExternalQA = "http://ws-qa.uberforever.eu:5000/";
    public const string BaseWebServiceUrlInternalDev = "http://ws-dev.uberforever.eu:5000/";

    public static readonly string WebPlayerFolder = Application.dataPath + "/../Latest/WebPlayer";
    public static readonly string WindowsStandaloneFolder = Application.dataPath + "/../Latest/WindowsStandalone";
    public static readonly string Windows64StandaloneFolder = Application.dataPath + "/../Latest/Windows64Standalone";
    public static readonly string OsxStandaloneFolder = Application.dataPath + "/../Latest/OsxStandalone";
    public static readonly string MacAppStoreStandaloneFolder = Application.dataPath + "/../Latest/MacAppStoreStandalone";
    public static readonly string AndroidFolder = Application.dataPath + "/../Latest/Android";
    public static readonly string IOSBuildFolder = Application.dataPath + "/../iOS/";

    public const string SceneFolder = "Assets/Scenes/";
    public const string SplashScene = "Splash";
    public const string SplashSceneWeb = "SplashWeb";
    public const string SplashSceneMobile = "SplashMobile";
    public const string MainScene = "Latest";
    public const string SpaceshipScene = "LevelSpaceship";
    public const string ClientVersion = "4.3.8";
    //TODO: read out version from ClientSdk 
    public const string ApiVersion = "4.3.7";

    // Make sure these entries match the current maps in instrumentation
    internal static IDictionary<int, string> MapsToExport = new Dictionary<int, string>()
    { 
        { 1, "LevelMonkeyIsland" },
        { 2, "LevelLostParadise2" },
        { 3, "LevelTheWarehouse" },
        { 4, "LevelTempleOfTheRaven" },
        { 5, "LevelFortWinter" },
        { 6, "LevelGideonsTower" },
        { 7, "LevelSkyGarden" },
        { 8, "LevelCuberStrike" },
        { 10, "LevelSpaceportAlpha" },
     };

    #region Android

    [MenuItem("File/Build UberStrike/Internal Dev/Android")]
    public static void BuildAndroidDev()
    {
        BuildAndroid();
    }

    [MenuItem("File/Build UberStrike/Internal Dev/Android (Main Only)")]
    public static void BuildAndroidDevMainOnly()
    {
        BuildAndroid(false);
    }

    public static void BuildAndroid(bool buildAllMaps = true, bool buildSplashLoader = false)
    {
        PrepareBuildFolder(AndroidFolder);
        BuildAndroidPlayer(AndroidFolder, buildAllMaps, buildSplashLoader, BuildOptions.None);
    }

    private static void BuildAndroidPlayer(string buildFolder, bool buildAllMaps, bool buildSplashLoader, BuildOptions buildOptions = BuildOptions.BuildAdditionalStreamedScenes)
    {
        string path = string.Empty;

        List<string> scenesToBuild = new List<string>();

        if (buildSplashLoader)
        {
            scenesToBuild.Add(SceneFolder + SplashSceneMobile + ".unity");
        }

        scenesToBuild.Add(SceneFolder + MainScene + ".unity");
        scenesToBuild.Add(SceneFolder + SpaceshipScene + ".unity");

        if (buildAllMaps)
        {
            //scenesToBuild.Add(SceneFolder + MapsToExport[1] + ".unity");
            //scenesToBuild.Add(SceneFolder + MapsToExport[2] + ".unity");
            scenesToBuild.Add(SceneFolder + MapsToExport[3] + ".unity");
            scenesToBuild.Add(SceneFolder + MapsToExport[4] + ".unity");
            scenesToBuild.Add(SceneFolder + MapsToExport[5] + ".unity");
            scenesToBuild.Add(SceneFolder + MapsToExport[6] + ".unity");
            scenesToBuild.Add(SceneFolder + MapsToExport[7] + ".unity");
            scenesToBuild.Add(SceneFolder + MapsToExport[8] + ".unity");
            scenesToBuild.Add(SceneFolder + MapsToExport[10] + ".unity");
        }

        // Setup the correct output path
        path = ApplicationDataManager.StandaloneFilename + ".apk";

        string buildResult = BuildPipeline.BuildPlayer(scenesToBuild.ToArray(), string.Format("{0}/{1}", buildFolder, path), BuildTarget.Android, buildOptions);
        if (!string.IsNullOrEmpty(buildResult))
        {
            Debug.LogError("BuildPlayer: " + buildResult);
            return;
        }
    }

    #endregion

    #region iOS

    [MenuItem("File/Build UberStrike/Internal Dev/iOS")]
    public static void BuildiOS()
    {
        BuildiOS(true, false);
    }

    [MenuItem("File/Build UberStrike/Internal Dev/iOS (Main Only)")]
    public static void BuildiOSMainOnly()
    {
        BuildiOS(false, false);
    }

    public static void BuildiOS(bool buildAllMaps, bool buildSplashLoader, BuildOptions buildOptions = BuildOptions.None)
    {
        List<string> scenesToBuild = new List<string>();

        if (buildSplashLoader)
        {
            scenesToBuild.Add(SceneFolder + SplashSceneMobile + ".unity");
        }

        scenesToBuild.Add(SceneFolder + MainScene + ".unity");
        scenesToBuild.Add(SceneFolder + SpaceshipScene + ".unity");

        if (buildAllMaps)
        {
            //scenesToBuild.Add(SceneFolder + MapsToExport[1] + ".unity");
            //scenesToBuild.Add(SceneFolder + MapsToExport[2] + ".unity");
            scenesToBuild.Add(SceneFolder + MapsToExport[3] + ".unity");
            scenesToBuild.Add(SceneFolder + MapsToExport[4] + ".unity");
            scenesToBuild.Add(SceneFolder + MapsToExport[5] + ".unity");
            scenesToBuild.Add(SceneFolder + MapsToExport[6] + ".unity");
            scenesToBuild.Add(SceneFolder + MapsToExport[7] + ".unity");
            scenesToBuild.Add(SceneFolder + MapsToExport[8] + ".unity");
            scenesToBuild.Add(SceneFolder + MapsToExport[10] + ".unity");
        }

        //PrepareiOSBuildFolder(IOSBuildFolder);
        string buildResult = BuildPipeline.BuildPlayer(scenesToBuild.ToArray(), IOSBuildFolder, BuildTarget.iPhone, buildOptions);
        if (!string.IsNullOrEmpty(buildResult))
        {
            Debug.LogError("BuildPlayer: " + buildResult);
            return;
        }
    }

    #endregion

    #region Mac App Store

    [MenuItem("File/Build UberStrike/Internal Dev/Mac App Store")]
    public static void BuildMacAppStoreStandaloneDev()
    {
        BuildMacAppStoreStandalone(true, true);
        WriteConfigurationXml(MacAppStoreStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + ".app/Contents/Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.MacAppStore);
    }

    [MenuItem("File/Build UberStrike/Internal Dev/Mac App Store (Main Only)")]
    public static void BuildMacAppStoreStandaloneDevMainOnly()
    {
        BuildMacAppStoreStandalone(false, true);
        WriteConfigurationXml(MacAppStoreStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + ".app/Contents/Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.MacAppStore);
    }

    [MenuItem("File/Build UberStrike/External QA/Mac App Store")]
    public static void BuildMacAppStoreStandaloneExternalQA()
    {
        BuildMacAppStoreStandalone(true, true);
        WriteConfigurationXml(MacAppStoreStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + ".app/Contents/Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Staging, WebserviceLocation.ExternalQA, ChannelType.MacAppStore);
    }

    [MenuItem("File/Build UberStrike/External QA/Mac App Store (Main Only)")]
    public static void BuildMacAppStoreStandaloneExternalQAMainOnly()
    {
        BuildMacAppStoreStandalone(false, false);
        WriteConfigurationXml(MacAppStoreStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + ".app/Contents/Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Staging, WebserviceLocation.ExternalQA, ChannelType.MacAppStore);
    }

    [MenuItem("File/Build UberStrike/Production/Mac App Store")]
    public static void BuildMacAppStoreStandaloneProd()
    {
        BuildMacAppStoreStandalone(true, false);
        WriteConfigurationXml(MacAppStoreStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + ".app/Contents/Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Prod, WebserviceLocation.Production, ChannelType.MacAppStore);
    }

    [MenuItem("File/Build UberStrike/Production/Mac App Store (Main Only)")]
    public static void BuildMacAppStoreStandaloneProdMainOnly()
    {
        BuildMacAppStoreStandalone(false, false);
        WriteConfigurationXml(MacAppStoreStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + ".app/Contents/Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Prod, WebserviceLocation.Production, ChannelType.MacAppStore);
    }

    public static void BuildMacAppStoreStandalone(bool buildAllMaps = true, bool useLogging = false)
    {
        PlayerSettings.useMacAppStoreValidation = true;
        PlayerSettings.usePlayerLog = useLogging;

        PrepareBuildFolder(MacAppStoreStandaloneFolder);
        BuildSplashPlayer(MacAppStoreStandaloneFolder, BuildTarget.StandaloneOSXIntel);
        BuildMainScene(MacAppStoreStandaloneFolder, BuildTarget.StandaloneOSXIntel);
        if (buildAllMaps) BuildMapScenes(MacAppStoreStandaloneFolder, BuildTarget.StandaloneOSXIntel);
    }

    #endregion

    #region OSX Standalone

    [MenuItem("File/Build UberStrike/Internal Dev/OSX Standalone Intel")]
    public static void BuildOsxStandaloneDev()
    {
        BuildOsxStandalone();

        WriteConfigurationXml(OsxStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + ".app/Contents/Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.OSXStandalone);
    }

    [MenuItem("File/Build UberStrike/Internal Dev/OSX Standalone Intel (Main Only)")]
    public static void BuildOsxStandaloneDevMain()
    {
        BuildOsxStandalone(false);

        WriteConfigurationXml(OsxStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + ".app/Contents/Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.OSXStandalone);
    }

    public static void BuildOsxStandalone(bool buildAllMaps = true)
    {
        PlayerSettings.useMacAppStoreValidation = false;
        PlayerSettings.usePlayerLog = true;

        PrepareBuildFolder(OsxStandaloneFolder);
        BuildSplashPlayer(OsxStandaloneFolder, BuildTarget.StandaloneOSXIntel);
        BuildMainScene(OsxStandaloneFolder, BuildTarget.StandaloneOSXIntel);
        if (buildAllMaps) BuildMapScenes(OsxStandaloneFolder, BuildTarget.StandaloneOSXIntel);
    }

    [MenuItem("File/Build UberStrike/Production/OSX Standalone")]
    public static void BuildOsxStandaloneProd()
    {
        // Prepare custom build options here
        PlayerSettings.useMacAppStoreValidation = false;
        PlayerSettings.usePlayerLog = true;

        PrepareBuildFolder(OsxStandaloneFolder);
        BuildSplashPlayer(OsxStandaloneFolder, BuildTarget.StandaloneOSXIntel);
        BuildMainScene(OsxStandaloneFolder, BuildTarget.StandaloneOSXIntel);
        BuildMapScenes(OsxStandaloneFolder, BuildTarget.StandaloneOSXIntel);
        WriteConfigurationXml(OsxStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + ".app/Contents/Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Prod, WebserviceLocation.Production, ChannelType.OSXStandalone);
    }

    [MenuItem("File/Build UberStrike/Production/OSX Standalone (Main Only)")]
    public static void BuildOsxStandaloneProdMainOnly()
    {
        // Prepare custom build options here
        PlayerSettings.useMacAppStoreValidation = false;
        PlayerSettings.usePlayerLog = true;

        PrepareBuildFolder(OsxStandaloneFolder);
        BuildSplashPlayer(OsxStandaloneFolder, BuildTarget.StandaloneOSXIntel);
        BuildMainScene(OsxStandaloneFolder, BuildTarget.StandaloneOSXIntel);

        WriteConfigurationXml(OsxStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + ".app/Contents/Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Prod, WebserviceLocation.Production, ChannelType.OSXStandalone);
    }

    #endregion

    #region Windows 64bit Standalone

    //[MenuItem("File/Build UberStrike/Internal Dev/Windows Standalone 64bit")]
    //public static void BuildWindows64StandaloneDev()
    //{
    //    PlayerSettings.useMacAppStoreValidation = false;
    //    PlayerSettings.usePlayerLog = true;

    //    PrepareBuildFolder(Windows64StandaloneFolder);
    //    BuildSplashPlayer(Windows64StandaloneFolder, BuildTarget.StandaloneWindows64);
    //    BuildMainScene(Windows64StandaloneFolder, BuildTarget.StandaloneWindows64);
    //    BuildMapScenes(Windows64StandaloneFolder, BuildTarget.StandaloneWindows64);

    //    WriteConfigurationXml(Windows64StandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + "_Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.WindowsStandalone);
    //}

    //[MenuItem("File/Build UberStrike/Internal Dev/Windows Standalone 64bit (Main Only)")]
    //public static void BuildWindows64StandaloneDevMain()
    //{
    //    PlayerSettings.useMacAppStoreValidation = false;
    //    PlayerSettings.usePlayerLog = true;

    //    PrepareBuildFolder(Windows64StandaloneFolder);
    //    BuildSplashPlayer(Windows64StandaloneFolder, BuildTarget.StandaloneWindows64);
    //    BuildMainScene(Windows64StandaloneFolder, BuildTarget.StandaloneWindows64);

    //    WriteConfigurationXml(Windows64StandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + "_Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.WindowsStandalone);
    //}

    //[MenuItem("File/Build UberStrike/Production/Windows Standalone 64bit (Main Only)")]
    //public static void BuildWindows64StandaloneProdMain()
    //{
    //    PlayerSettings.useMacAppStoreValidation = false;
    //    PlayerSettings.usePlayerLog = true;

    //    PrepareBuildFolder(Windows64StandaloneFolder);
    //    BuildSplashPlayer(Windows64StandaloneFolder, BuildTarget.StandaloneWindows64);
    //    BuildMainScene(Windows64StandaloneFolder, BuildTarget.StandaloneWindows64);

    //    WriteConfigurationXml(Windows64StandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + "_Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Prod, WebserviceLocation.Production, ChannelType.WindowsStandalone);
    //}

    #endregion

    #region Windows Standalone

    [MenuItem("File/Build UberStrike/Internal Dev/Windows Standlone")]
    public static void BuildWindowsStandaloneDev()
    {
        BuildWindowsStandalone();

        WriteConfigurationXml(WindowsStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + "_Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.WindowsStandalone);
    }

    [MenuItem("File/Build UberStrike/External QA/Windows Standalone (Main Only)")]
    public static void BuildWindowsStandaloneDevMain()
    {
        BuildWindowsStandalone(false);

        WriteConfigurationXml(WindowsStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + "_Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.WindowsStandalone);
    }

    [MenuItem("File/Build UberStrike/External QA/Windows Standlone")]
    public static void BuildWindowsStandaloneQa()
    {
        BuildWindowsStandalone();

        WriteConfigurationXml(WindowsStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + "_Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Staging, WebserviceLocation.ExternalQA, ChannelType.WindowsStandalone);
    }

    [MenuItem("File/Build UberStrike/Internal Dev/Windows Standalone (Main Only)")]
    public static void BuildWindowsStandaloneQaMain()
    {
        BuildWindowsStandalone(false);

        WriteConfigurationXml(WindowsStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + "_Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Staging, WebserviceLocation.ExternalQA, ChannelType.WindowsStandalone);
    }

    [MenuItem("File/Build UberStrike/Production/Windows Standalone (Main Only)")]
    public static void BuildWindowsStandaloneProdMain()
    {
        BuildWindowsStandalone(false);

        WriteConfigurationXml(WindowsStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + "_Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Prod, WebserviceLocation.Production, ChannelType.WindowsStandalone);
    }

    [MenuItem("File/Build UberStrike/Production/Windows Standalone")]
    public static void BuildWindowsStandaloneProd()
    {
        BuildWindowsStandalone();

        WriteConfigurationXml(WindowsStandaloneFolder + "/" + ApplicationDataManager.StandaloneFilename + "_Data/" + ApplicationDataManager.StandaloneFilename + ".xml", BuildType.Prod, WebserviceLocation.Production, ChannelType.WindowsStandalone);
    }

    public static void BuildWindowsStandalone(bool buildMaps = true)
    {
        PlayerSettings.useMacAppStoreValidation = false;
        PlayerSettings.usePlayerLog = true;

        PrepareBuildFolder(WindowsStandaloneFolder);
        BuildSplashPlayer(WindowsStandaloneFolder, BuildTarget.StandaloneWindows);
        BuildMainScene(WindowsStandaloneFolder, BuildTarget.StandaloneWindows);
        if (buildMaps) BuildMapScenes(WindowsStandaloneFolder, BuildTarget.StandaloneWindows);
    }

    #endregion

    #region Web Player

    [MenuItem("File/Build UberStrike/Internal Dev/Web Player (Local Debug)")]
    public static void BuildWebPlayerDevLocal()
    {
        BuildWebPlayer(false);
        WriteConfigurationXml(string.Format("{0}/{1}.xml", WebPlayerFolder, ApplicationDataManager.HeaderFilename), BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.WebPortal, true);
        Debug.Log("Finished Web Player Build.");
    }

    [MenuItem("File/Build UberStrike/Internal Dev/Web Player")]
    public static void BuildWebPlayerDev()
    {
        BuildWebPlayer();
        WriteConfigurationXml(string.Format("{0}/{1}.xml", WebPlayerFolder, ApplicationDataManager.HeaderFilename), BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.WebPortal);
        Debug.Log("Finished Web Player Build.");
    }

    [MenuItem("File/Build UberStrike/External QA/Web Player")]
    public static void BuildWebPlayerExternalQA()
    {
        BuildWebPlayer();
        WriteConfigurationXml(string.Format("{0}/{1}.xml", WebPlayerFolder, ApplicationDataManager.HeaderFilename), BuildType.Staging, WebserviceLocation.ExternalQA, ChannelType.WebPortal);
        Debug.Log("Finished Web Player Build.");
    }

    [MenuItem("File/Build UberStrike/Production/Web Player")]
    public static void BuildWebPlayerProduction()
    {
        BuildWebPlayerMain(BuildType.Prod, WebserviceLocation.Production);
    }

    [MenuItem("File/Build UberStrike/Internal Dev/Web Player (Main Only)")]
    public static void BuildWebPlayerDevMain()
    {
        BuildWebPlayerMain(BuildType.Dev, WebserviceLocation.InternalDev);
    }

    [MenuItem("File/Build UberStrike/External QA/Web Player (Main Only)")]
    public static void BuildWebPlayerExternalQAMain()
    {
        BuildWebPlayerMain(BuildType.Staging, WebserviceLocation.ExternalQA);
    }

    [MenuItem("File/Build UberStrike/Production/Web Player (Main Only)")]
    public static void BuildWebPlayerProductionMain()
    {
        BuildWebPlayerMain(BuildType.Prod, WebserviceLocation.Production);
    }

    #endregion

    #region Export Maps

    [MenuItem("File/Build UberStrike/Maps/Monkey Island")]
    public static void BuildMapMI()
    {
        PrepareBuildFolder(Application.dataPath + "/../Latest/Maps");
        BuildMapScene(Application.dataPath + "/../Latest/Maps", BuildTarget.WebPlayer, MapsToExport[1]);
    }

    [MenuItem("File/Build UberStrike/Maps/Lost Paradise 2")]
    public static void BuildMapLP2()
    {
        PrepareBuildFolder(Application.dataPath + "/../Latest/Maps");
        BuildMapScene(Application.dataPath + "/../Latest/Maps", BuildTarget.WebPlayer, MapsToExport[2]);
    }

    [MenuItem("File/Build UberStrike/Maps/The Warehouse")]
    public static void BuildMapTW()
    {
        PrepareBuildFolder(Application.dataPath + "/../Latest/Maps");
        BuildMapScene(Application.dataPath + "/../Latest/Maps", BuildTarget.WebPlayer, MapsToExport[3]);
    }

    [MenuItem("File/Build UberStrike/Maps/Temple of the Raven")]
    public static void BuildMapTOR()
    {
        PrepareBuildFolder(Application.dataPath + "/../Latest/Maps");
        BuildMapScene(Application.dataPath + "/../Latest/Maps", BuildTarget.WebPlayer, MapsToExport[4]);
    }

    [MenuItem("File/Build UberStrike/Maps/Fort Winter")]
    public static void BuildMapFW()
    {
        PrepareBuildFolder(Application.dataPath + "/../Latest/Maps");
        BuildMapScene(Application.dataPath + "/../Latest/Maps", BuildTarget.WebPlayer, MapsToExport[5]);
    }

    [MenuItem("File/Build UberStrike/Maps/Gideons Tower")]
    public static void BuildMapGT()
    {
        PrepareBuildFolder(Application.dataPath + "/../Latest/Maps");
        BuildMapScene(Application.dataPath + "/../Latest/Maps", BuildTarget.WebPlayer, MapsToExport[6]);
    }

    [MenuItem("File/Build UberStrike/Maps/Sky Garden")]
    public static void BuildMapSG()
    {
        PrepareBuildFolder(Application.dataPath + "/../Latest/Maps");
        BuildMapScene(Application.dataPath + "/../Latest/Maps", BuildTarget.WebPlayer, MapsToExport[7]);
    }

    [MenuItem("File/Build UberStrike/Maps/CuberStrike")]
    public static void BuildMapCS()
    {
        PrepareBuildFolder(Application.dataPath + "/../Latest/Maps");
        BuildMapScene(Application.dataPath + "/../Latest/Maps", BuildTarget.WebPlayer, MapsToExport[8]);
    }

    [MenuItem("File/Build UberStrike/Maps/Lost Paradise Classic")]
    public static void BuildMapLPC()
    {
        PrepareBuildFolder(Application.dataPath + "/../Latest/Maps");
        BuildMapScene(Application.dataPath + "/../Latest/Maps", BuildTarget.WebPlayer, MapsToExport[9]);
    }

    [MenuItem("File/Build UberStrike/Maps/Spaceport Alpha")]
    public static void BuildMapSPA()
    {
        PrepareBuildFolder(Application.dataPath + "/../Latest/Maps");
        BuildMapScene(Application.dataPath + "/../Latest/Maps", BuildTarget.WebPlayer, MapsToExport[10]);
    }


    [MenuItem("File/Build UberStrike/Maps/Spaceship")]
    public static void BuildMapSS()
    {
        PrepareBuildFolder(Application.dataPath + "/../Latest/Maps");
        BuildMapScene(Application.dataPath + "/../Latest/Maps", BuildTarget.WebPlayer, "LevelSpaceship");
    }

    [MenuItem("File/Build UberStrike/Maps/Latest")]
    public static void BuildMapLatest()
    {
        PrepareBuildFolder(Application.dataPath + "/../Latest/Maps");
        BuildMapScene(Application.dataPath + "/../Latest/Maps", BuildTarget.WebPlayer, "Latest");
    }

    #endregion

    #region Build & Export Methods

    public static void BuildWebPlayer(bool enableLevelHashing = true, bool buildMaps = true)
    {
        PrepareBuildFolder(WebPlayerFolder);

        BuildSplashPlayer(WebPlayerFolder, BuildTarget.WebPlayer);
        BuildMainScene(WebPlayerFolder, BuildTarget.WebPlayer);
        if (buildMaps) BuildMapScenes(WebPlayerFolder, BuildTarget.WebPlayer, enableLevelHashing);

        // Move the Header up a folder, next to the MainScene
        if (Directory.Exists(string.Format("{0}/{1}", WebPlayerFolder, ApplicationDataManager.HeaderFilename)))
        {
            FileUtil.MoveFileOrDirectory(string.Format("{0}/{1}/{1}.html", WebPlayerFolder, ApplicationDataManager.HeaderFilename), string.Format("{0}/{1}.html", WebPlayerFolder, ApplicationDataManager.HeaderFilename));
            FileUtil.MoveFileOrDirectory(string.Format("{0}/{1}/{1}.unity3d", WebPlayerFolder, ApplicationDataManager.HeaderFilename), string.Format("{0}/{1}.unity3d", WebPlayerFolder, ApplicationDataManager.HeaderFilename));
            FileUtil.DeleteFileOrDirectory(string.Format("{0}/{1}", WebPlayerFolder, ApplicationDataManager.HeaderFilename));
        }

        if (Directory.Exists(string.Format("{0}/{1}.unity3d", WebPlayerFolder, ApplicationDataManager.MainFilename)))
        {
            FileUtil.MoveFileOrDirectory(string.Format("{0}/{1}.unity3d", WebPlayerFolder, ApplicationDataManager.MainFilename), string.Format("{0}/{1}.unity3d", WebPlayerFolder, ApplicationDataManager.MainFilename));
        }
    }

    public static void BuildWebPlayerMain(BuildType buildType, WebserviceLocation wsLocation)
    {
        PrepareBuildFolder(WebPlayerFolder);
        BuildSplashPlayer(WebPlayerFolder, BuildTarget.WebPlayer);
        BuildMainScene(WebPlayerFolder, BuildTarget.WebPlayer);

        // Move the Header up a folder, next to the MainScene
        if (Directory.Exists(string.Format("{0}/{1}", WebPlayerFolder, ApplicationDataManager.HeaderFilename)))
        {
            FileUtil.MoveFileOrDirectory(string.Format("{0}/{1}/{1}.html", WebPlayerFolder, ApplicationDataManager.HeaderFilename), string.Format("{0}/{1}.html", WebPlayerFolder, ApplicationDataManager.HeaderFilename));
            FileUtil.MoveFileOrDirectory(string.Format("{0}/{1}/{1}.unity3d", WebPlayerFolder, ApplicationDataManager.HeaderFilename), string.Format("{0}/{1}.unity3d", WebPlayerFolder, ApplicationDataManager.HeaderFilename));
            FileUtil.DeleteFileOrDirectory(string.Format("{0}/{1}", WebPlayerFolder, ApplicationDataManager.HeaderFilename));
        }

        if (Directory.Exists(string.Format("{0}/{1}.unity3d", WebPlayerFolder, ApplicationDataManager.MainFilename)))
        {
            FileUtil.MoveFileOrDirectory(string.Format("{0}/{1}.unity3d", WebPlayerFolder, ApplicationDataManager.MainFilename), string.Format("{0}/{1}.unity3d", WebPlayerFolder, ApplicationDataManager.MainFilename));
        }

        WriteConfigurationXml(string.Format("{0}/{1}.xml", WebPlayerFolder, ApplicationDataManager.HeaderFilename), buildType, wsLocation, ChannelType.WebPortal);

        Debug.Log("Finished Web Player Build.");
    }

    private static void BuildSplashPlayer(string buildFolder, BuildTarget buildTarget)
    {
        string scenename = string.Empty;
        string filename = string.Empty;

        // Setup the correct output filename
        switch (buildTarget)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                filename = ApplicationDataManager.StandaloneFilename + ".exe";
                scenename = SplashScene;
                break;
            case BuildTarget.StandaloneOSXIntel:
                filename = ApplicationDataManager.StandaloneFilename + ".app";
                scenename = SplashScene;
                break;
            case BuildTarget.WebPlayer:
            case BuildTarget.WebPlayerStreamed:
                filename = ApplicationDataManager.HeaderFilename;
                scenename = SplashSceneWeb;
                break;
        }

        string buildResult = BuildPipeline.BuildPlayer(new string[] { SceneFolder + scenename + ".unity" }, string.Format("{0}/{1}", buildFolder, filename), buildTarget, BuildOptions.ConnectWithProfiler | BuildOptions.AllowDebugging);
        if (!string.IsNullOrEmpty(buildResult))
        {
            Debug.LogError("BuildPlayer: " + buildResult);
            return;
        }
    }

    private static void BuildMainScene(string buildFolder, BuildTarget buildTarget, BuildOptions buildOptions = BuildOptions.BuildAdditionalStreamedScenes)
    {
        string path = string.Empty;

        // Setup the correct output path
        switch (buildTarget)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                path = ApplicationDataManager.StandaloneFilename + "_Data/" + ApplicationDataManager.MainFilename;
                break;
            case BuildTarget.StandaloneOSXIntel:
                path = ApplicationDataManager.StandaloneFilename + ".app/Contents/Data/" + ApplicationDataManager.MainFilename;
                break;
            case BuildTarget.WebPlayer:
            case BuildTarget.WebPlayerStreamed:
                path = ApplicationDataManager.MainFilename;
                break;
            case BuildTarget.Android:
                path = ApplicationDataManager.StandaloneFilename + ".apk";
                break;
        }

        string buildResult = BuildPipeline.BuildPlayer(new string[] { SceneFolder + MainScene + ".unity", SceneFolder + SpaceshipScene + ".unity" }, string.Format("{0}/{1}.unity3d", buildFolder, path), buildTarget, buildOptions);
        if (!string.IsNullOrEmpty(buildResult))
        {
            Debug.LogError("BuildPlayer: " + buildResult);
            return;
        }
    }

    private static void BuildMapScenes(string buildFolder, BuildTarget buildTarget, bool enableLevelHashing = true)
    {
        string path = string.Empty;
        string mapTypeSuffix = string.Empty;
        // Setup the correct output path
        switch (buildTarget)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                path = ApplicationDataManager.StandaloneFilename + "_Data/";
                mapTypeSuffix = "HD";
                break;
            case BuildTarget.StandaloneOSXIntel:
                path = ApplicationDataManager.StandaloneFilename + ".app/Contents/Data/";
                mapTypeSuffix = "HD";
                break;
            default:
                mapTypeSuffix = "SD";
                break;
        }

        // Build Level scenes
        foreach (var map in MapsToExport)
        {
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(SceneFolder + map.Value + ".unity", typeof(UnityEngine.Object));
            if (obj != null)
            {
                Debug.Log("Build Map: " + map.Value);
                string buildResult = BuildPipeline.BuildPlayer(new string[] { SceneFolder + map.Value + ".unity" }, string.Format("{0}/{1}.unity3d", buildFolder, path + map.Value), buildTarget, BuildOptions.BuildAdditionalStreamedScenes);

                if (!string.IsNullOrEmpty(buildResult))
                {
                    Debug.LogError("BuildPlayer: " + buildResult);
                }
                else
                {
                    if (enableLevelHashing)
                    {
                        string hash = CreateMd5Checksum(SceneFolder + map.Value + ".unity");

                        string mapAssetBundleName = string.Format("{0}/{1}Map-{2:00}-{3}-{4}.unity3d", buildFolder, path, map.Key, hash, mapTypeSuffix);
                        Debug.Log("Built Map AssetBundle. Filename=" + mapAssetBundleName);
                        File.Move(string.Format("{0}/{1}.unity3d", buildFolder, path + map.Value), mapAssetBundleName);
                    }
                    else
                    {
                        string levelPath = string.Format("{0}/{1}.unity3d", buildFolder, path + map.Value);
                        Debug.Log("Built Non-Hashed level at: " + levelPath);
                    }
                }
            }
            else
            {
                Debug.LogWarning("BuildPlayer Error: Map not found with name = " + map.Value);
            }
        }
    }

    private static void BuildMapScene(string buildFolder, BuildTarget buildTarget, string sceneName)
    {
        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(SceneFolder + sceneName + ".unity", typeof(UnityEngine.Object));

        if (obj != null)
        {
            Debug.Log("Build Map: " + sceneName);
            string buildResult = BuildPipeline.BuildPlayer(new string[] { SceneFolder + sceneName + ".unity" }, string.Format("{0}/{1}.unity3d", buildFolder, sceneName), buildTarget, BuildOptions.BuildAdditionalStreamedScenes);

            if (!string.IsNullOrEmpty(buildResult))
            {
                Debug.LogError("BuildPlayer: " + buildResult);
            }
            else
            {
                Debug.Log("Built level at: " + string.Format("{0}/{1}.unity3d", buildFolder, sceneName));
            }
        }
        else
        {
            Debug.LogWarning("BuildPlayer Error: Map not found with name = " + sceneName);
        }
    }

    #endregion

    #region Helper Methods

    private static void PrepareBuildFolder(string exportFolder)
    {
        if (!Directory.Exists(Application.dataPath + "/../Latest")) Directory.CreateDirectory(Application.dataPath + "/../Latest");
        if (Directory.Exists(exportFolder)) FileUtil.DeleteFileOrDirectory(exportFolder);
        Directory.CreateDirectory(exportFolder);
    }

    private static void PrepareiOSBuildFolder(string exportFolder)
    {
        //if (Directory.Exists(exportFolder)) FileUtil.DeleteFileOrDirectory(exportFolder);
        if (!Directory.Exists(exportFolder))
            Directory.CreateDirectory(exportFolder);
    }

    #region Create Local Configuration XML

    [MenuItem("File/Create Editor Configuration Xml/Local/Mac App Store")]
    private static void CreateConfigurationXmlLocalMAS() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Dev, WebserviceLocation.Localhost, ChannelType.MacAppStore); }

    [MenuItem("File/Create Editor Configuration Xml/Local/OSX Standalone")]
    private static void CreateConfigurationXmlLocalOSX() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Dev, WebserviceLocation.Localhost, ChannelType.OSXStandalone); }

    [MenuItem("File/Create Editor Configuration Xml/Local/Web Facebook")]
    private static void CreateConfigurationXmlLocalFacebook() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Dev, WebserviceLocation.Localhost, ChannelType.WebFacebook); }

    [MenuItem("File/Create Editor Configuration Xml/Local/Web Kongregate")]
    private static void CreateConfigurationXmlLocalKongregate() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Dev, WebserviceLocation.Localhost, ChannelType.Kongregate); }

    [MenuItem("File/Create Editor Configuration Xml/Local/Web Portal")]
    private static void CreateConfigurationXmlLocalPortal() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Dev, WebserviceLocation.Localhost, ChannelType.WebPortal); }

    [MenuItem("File/Create Editor Configuration Xml/Local/Windows Standalone")]
    private static void CreateConfigurationXmlLocalWindows() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Dev, WebserviceLocation.Localhost, ChannelType.WindowsStandalone); }

    #endregion

    #region Create Dev Configuration XML

    [MenuItem("File/Create Editor Configuration Xml/Internal Dev/Mac App Store")]
    private static void CreateConfigurationXmlInternalDevMAS() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.MacAppStore); }

    [MenuItem("File/Create Editor Configuration Xml/Internal Dev/OSX Standalone")]
    private static void CreateConfigurationXmlInternalDevOSX() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.OSXStandalone); }

    [MenuItem("File/Create Editor Configuration Xml/Internal Dev/Web Facebook")]
    private static void CreateConfigurationXmlInternalDevFacebook() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.WebFacebook); }

    [MenuItem("File/Create Editor Configuration Xml/Internal Dev/Web Kongregate")]
    private static void CreateConfigurationXmlInternalDevKongregate() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.Kongregate); }

    [MenuItem("File/Create Editor Configuration Xml/Internal Dev/Web Portal")]
    private static void CreateConfigurationXmlInternalDevPortal() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.WebPortal); }

    [MenuItem("File/Create Editor Configuration Xml/Internal Dev/Windows Standalone")]
    private static void CreateConfigurationXmlInternalDevWindows() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.WindowsStandalone); }

    [MenuItem("File/Create Editor Configuration Xml/Internal Dev/Android")]
    private static void CreateConfigurationXmlInternalDevAndroid() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Dev, WebserviceLocation.InternalDev, ChannelType.Android); }

    #endregion

    #region Create External QA Configuration XML

    [MenuItem("File/Create Editor Configuration Xml/External QA/Mac App Store")]
    private static void CreateConfigurationXmlExternalQAMAS() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Staging, WebserviceLocation.ExternalQA, ChannelType.MacAppStore); }

    [MenuItem("File/Create Editor Configuration Xml/External QA/OSX Standalone")]
    private static void CreateConfigurationXmlExternalQAOSX() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Staging, WebserviceLocation.ExternalQA, ChannelType.OSXStandalone); }

    [MenuItem("File/Create Editor Configuration Xml/External QA/Web Facebook")]
    private static void CreateConfigurationXmlExternalQAFacebook() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Staging, WebserviceLocation.ExternalQA, ChannelType.WebFacebook); }

    [MenuItem("File/Create Editor Configuration Xml/External QA/Web Portal")]
    private static void CreateConfigurationXmlExternalQAPortal() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Staging, WebserviceLocation.ExternalQA, ChannelType.WebPortal); }

    [MenuItem("File/Create Editor Configuration Xml/External QA/Windows Standalone")]
    private static void CreateConfigurationXmlExternalQAWindows() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Staging, WebserviceLocation.ExternalQA, ChannelType.WindowsStandalone); }

    #endregion

    #region Create Prod Configuration XML

    [MenuItem("File/Create Editor Configuration Xml/Production/Mac App Store")]
    private static void CreateConfigurationXmlProductionMAS() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Prod, WebserviceLocation.Production, ChannelType.MacAppStore); }

    [MenuItem("File/Create Editor Configuration Xml/Production/OSX Standalone")]
    private static void CreateConfigurationXmlProductionOSX() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Prod, WebserviceLocation.Production, ChannelType.OSXStandalone); }

    [MenuItem("File/Create Editor Configuration Xml/Production/Web Facebook")]
    private static void CreateConfigurationXmlProductionFacebook() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Prod, WebserviceLocation.Production, ChannelType.WebFacebook); }

    [MenuItem("File/Create Editor Configuration Xml/Production/Web Portal")]
    private static void CreateConfigurationXmlProductionPortal() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Prod, WebserviceLocation.Production, ChannelType.WebPortal); }

    [MenuItem("File/Create Editor Configuration Xml/Production/Windows Standalone")]
    private static void CreateConfigurationXmlProductionWindows() { WriteConfigurationXml(Application.dataPath + "/../EditorConfiguration.xml", BuildType.Prod, WebserviceLocation.Production, ChannelType.WindowsStandalone); }

    #endregion

    private static void WriteConfigurationXml(string path, BuildType buildType, WebserviceLocation webserviceLocation, ChannelType channelType, bool runLocally = false)
    {
        WriteConfigurationXml(new ClientConfiguration()
        {
            BuildType = buildType,
            DebugLevel = DebugLevel.Debug,
            Version = ClientVersion,
            WebServiceBaseUrl = GetWebserviceBaseUrl(webserviceLocation),
            ContentBaseUrl = GetContentBaseUrl(webserviceLocation),
            ChannelType = channelType,
            IsLocalWebplayer = runLocally
        }, path);
    }

    public static void WriteConfigurationXml(ClientConfiguration clientConfiguration, string configurationXmlFilePath)
    {
        if (!string.IsNullOrEmpty(configurationXmlFilePath) &&
        configurationXmlFilePath.ToLower().Contains(".xml"))
        {
            XmlWriter xml = XmlWriter.Create(configurationXmlFilePath, new XmlWriterSettings() { Indent = true, NewLineOnAttributes = true, Encoding = Encoding.ASCII });

            try
            {
                xml.WriteStartDocument();

                xml.WriteStartElement("UberStrike");
                xml.WriteStartElement("Application");

                xml.WriteAttributeString("BuildType", clientConfiguration.BuildType.ToString());
                xml.WriteAttributeString("DebugLevel", clientConfiguration.DebugLevel.ToString());
                xml.WriteAttributeString("Version", clientConfiguration.Version);
                xml.WriteAttributeString("WebServiceBaseUrl", clientConfiguration.WebServiceBaseUrl);
                xml.WriteAttributeString("ContentBaseUrl", clientConfiguration.ContentBaseUrl);
                xml.WriteAttributeString("ChannelType", clientConfiguration.ChannelType.ToString());
                if (clientConfiguration.IsLocalWebplayer)
                    xml.WriteAttributeString("LocalWebplayer", true.ToString());

                xml.WriteEndElement();
                xml.WriteEndElement();

                xml.WriteEndDocument();
            }
            finally
            {
                xml.Flush();
                xml.Close();
            }
        }
        else
        {
            Debug.LogError("Invalid url supplied for the Configuration XML file.\n'" + configurationXmlFilePath + "'");
        }
    }

    private static string CreateMd5Checksum(string filename)
    {
        StringBuilder sBuilder = new StringBuilder();
        FileStream stream = File.OpenRead(filename);

        System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();

        try
        {
            byte[] data = x.ComputeHash(stream);

            // create a hexadecimal string
            for (int i = 0; i < data.Length; i++)
                sBuilder.Append(data[i].ToString("x2"));
        }
        finally
        {
            stream.Close();
        }

        return sBuilder.ToString();
    }

    public static string GetContentBaseUrl(WebserviceLocation location)
    {
        switch (location)
        {
            case WebserviceLocation.Production:
                return BaseContentUrlProduction;
            case WebserviceLocation.ExternalQA:
                return BaseContentUrlExternalQA;
            case WebserviceLocation.InternalDev:
                return BaseContentUrlInternalDev;
            case WebserviceLocation.Localhost:
                return BaseContentUrlInternalDev;
            default:
                return BaseContentUrlProduction;
        }
    }

    public static string GetWebserviceBaseUrl(WebserviceLocation location)
    {
        switch (location)
        {
            case WebserviceLocation.Production:
                return BaseWebServiceUrlProduction;
            case WebserviceLocation.ExternalQA:
                return BaseWebServiceUrlExternalQA;
            case WebserviceLocation.InternalDev:
                return BaseWebServiceUrlInternalDev;
            case WebserviceLocation.Localhost:
                return "http://localhost:9000/";
            default:
                return BaseWebServiceUrlProduction;
        }
    }

    #endregion
}
