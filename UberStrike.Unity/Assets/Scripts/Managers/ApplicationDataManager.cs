using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Cmune.DataCenter.Common.Entities;
using Cmune.Realtime.Photon.Client.Utils;
using Cmune.Util;
using UberStrike.Core.Types;
using UberStrike.DataCenter.Common.Entities;
using UberStrike.WebService.Unity;
using UnityEngine;
using Cmune.Core.Models.Views;

public class ApplicationDataManager : MonoSingleton<ApplicationDataManager>
{
    [SerializeField]
    private bool loadMapsUsingWebService;
    [SerializeField]
    private LocaleType _locale = LocaleType.en_US;

    public const string HeaderFilename = "UberStrikeHeader";
    public const string MainFilename = "UberStrikeMain";
    public const string StandaloneFilename = "UberStrike";

    public const int MinimalWidth = 989;
    public const int MinimalHeight = 560;

    public static ChannelType Channel { get; set; }
    public static string VersionLong = string.Empty;
    public static string VersionShort = string.Empty;
    public static BuildType BuildType;
    public static bool IsMobile
    {
        get
        {
            return Channel == ChannelType.Android || Channel == ChannelType.IPad || Channel == ChannelType.IPhone;
        }
    }

    private static CmuneSystemInfo localSystemInfo;

    private static ApplicationOptions applicationOptions = new ApplicationOptions();
    public static readonly Dictionary<int, int> XpByLevel = new Dictionary<int, int>();
    private string clientConfigurationXml = string.Empty;
    private static float applicationDateTime = 0f;
    private static DateTime serverDateTime = DateTime.Now;
    private static ProgressPopupDialog initApplicationProgressPopup;

    public static string BaseAudioURL { get; private set; }
    public static string BaseMapsURL { get; private set; }
    public static string BaseImageURL { get; private set; }
    public static string BaseStandaloneMapsURL { get; private set; }
    public static bool IsOnline { get; set; }
    public static bool AutoLogin = true;
    public static WebPlayerSrcValues WebPlayerSrcValues { get; private set; }

    public static LocaleType CurrentLocale { get; private set; }
    public static string CurrentLocaleString { get { return LocalizationHelper.GetLocaleString(CurrentLocale); } }

#if UNITY_IPHONE
    // AOT Helper - Construct classes that are only referenced using reflection here.
    private void AOTHelperMethod()
    {
        var temp = new System.Collections.Generic.SortedList<int, UberStrike.Core.ViewModel.ItemTransactionsViewModel>();
        var temp1 = new System.Collections.Generic.SortedList<int, UberStrike.Core.ViewModel.PointDepositsViewModel>();
        var temp2 = new System.Collections.Generic.SortedList<int, UberStrike.Core.ViewModel.CurrencyDepositsViewModel>();
    }
#endif

    private void Awake()
    {
        // Configure Unity player options
        Application.runInBackground = true;

        // Setup debugging information
        CmuneDebug.DebugFileInfo = true;
        CmuneDebug.DebugLevel = 0;
        CmuneDebug.AddDebugChannel(new UnityDebug());

        Cryptography.Policy = new CryptographyPolicy();

        // Kick off the coroutine to calc screen size
        StartCoroutine(GUITools.StartScreenSizeListener(1));

        // Setup the Local System Info
        localSystemInfo = new CmuneSystemInfo();

        // Get the web player url parameters
        WebPlayerSrcValues = new WebPlayerSrcValues(WWW.UnEscapeURL(Application.absoluteURL, Encoding.UTF8));

        //turn off the authentication procedure when in offline mode
        enabled = AutoLogin;

        if (_locale != LocaleType.en_US)
            LocalizedStrings.UpdateLocalization(_locale);
    }

#if UNITY_ANDROID
    private string mobileConfigXml = @"<?xml version=""1.0"" encoding=""us-ascii""?>
                                        <UberStrike>
                                          <Application
                                            BuildType=""Prod""
                                            DebugLevel=""Error""
                                            Version=""4.3.8""
                                            WebServiceBaseUrl=""http://ws.uberstrike.cmune.com/1.0.0/""
                                            ContentBaseUrl=""http://distro.client.cloud.cmune.com/UberStrike""
                                            ChannelType=""Android"" />
                                        </UberStrike>";

#endif

#if UNITY_IPHONE
    private string mobileConfigXml = @"<?xml version=""1.0"" encoding=""us-ascii""?>
                                        <UberStrike>
                                          <Application
                                            BuildType=""Prod""
                                            DebugLevel=""Error""
                                            Version=""4.3.8""
                                            WebServiceBaseUrl=""http://ws.uberstrike.cmune.com/1.0.0/""
                                            ContentBaseUrl=""http://distro.client.cloud.cmune.com/UberStrike""
                                            ChannelType=""IPad"" />
                                        </UberStrike>";

#endif

#if UNITY_ANDROID || UNITY_IPHONE

    [SerializeField]
    private Texture2D backgroundTexture;

    private float _backgroundAlpha = 1.0f;

    private bool _showLoadingBackground;

    private void OnGUI()
    {
        if (_showLoadingBackground)
        {
            GUI.depth = 10;
            GUI.DrawTexture(new Rect(0, 0, backgroundTexture.width, backgroundTexture.height), backgroundTexture);
        }
        else if (_backgroundAlpha > 0.01f)
        {
            _backgroundAlpha = Mathf.Lerp(_backgroundAlpha, 0, Time.deltaTime * 2);
            GUI.color = new Color(1, 1, 1, _backgroundAlpha);
            GUI.depth = 10;
            GUI.DrawTexture(new Rect(0, 0, backgroundTexture.width, backgroundTexture.height), backgroundTexture);
            GUI.color = Color.white;
        }
    }

#endif

    private IEnumerator Start()
    {
#if UNITY_ANDROID || UNITY_IPHONE
        _showLoadingBackground = true;
#endif
        if (PopupSystem.IsAnyPopupOpen)
            PopupSystem.ClearAll();

        initApplicationProgressPopup = PopupSystem.ShowProgress("Initializing", "Connecting to UberStrike...");
        initApplicationProgressPopup.ManualProgress = 0.1f;

        // Write the step tracking info (Game Loaded)
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Application.ExternalCall("wsTracking", ((int)UserInstallStepType.FullGameLoaded).ToString());
            Application.ExternalEval(_evalBrwJS);
        }

#if UNITY_EDITOR
        if (!File.Exists(Application.dataPath + "/../EditorConfiguration.xml"))
        {
            HandleApplicationAuthenticationError("The EditorConfiguration.xml file was not found in your Unity project folder.\nTo create it, select 'File > Create Editor Configuration Xml' and select the configuration and channel type.");
            yield break;
        }
#endif

#if UNITY_ANDROID || UNITY_IPHONE
        CmuneDebug.LogWarning("Loading Android/iOS XML Configuration from internal settings.");
        clientConfigurationXml = mobileConfigXml;
#else
        yield return StartCoroutine(LoadConfigurationXml(GetConfigurationXmlFilePath()));
#endif

        initApplicationProgressPopup.ManualProgress = 0.3f;

        if (string.IsNullOrEmpty(clientConfigurationXml))
        {
            HandleConfigurationMissingError("The Client Configuration XML was null or empty. Uberstrike cannot continue.");
        }
        else
        {
            ClientConfiguration clientConfiguration = this.ParseConfigurationXml(this.clientConfigurationXml);

            if (clientConfiguration == null)
            {
                HandleConfigurationMissingError("The ClientConfiguration object was null. Uberstrike cannot continue.");
            }
            else
            {
                Configuration.WebserviceBaseUrl = clientConfiguration.WebServiceBaseUrl;
                BaseAudioURL = clientConfiguration.ContentBaseUrl + "Audio/";
                BaseImageURL = clientConfiguration.ContentBaseUrl + "Images/";
                BaseMapsURL = clientConfiguration.ContentBaseUrl + "Maps/" + clientConfiguration.Version + "/";
                BuildType = clientConfiguration.BuildType;
                VersionShort = clientConfiguration.Version;
                CmuneDebug.DebugLevel = (int)clientConfiguration.DebugLevel;
                SetChannel(clientConfiguration);
                SetVersionLong(clientConfiguration);
                SetBaseStandaloneMapsURL();

                if (clientConfiguration.IsLocalWebplayer)
                {
                    LevelManager.Instance.SimulateWebplayer(Application.dataPath);
                }

                // Setup Application Options
                applicationOptions.Initialize();

#if !UNITY_ANDROID && !UNITY_IPHONE
                // Set the initial Video options based on Cmune Prefs
                if (applicationOptions.IsUsingCustom)
                {
                    QualitySettings.globalTextureMipmapLimit = applicationOptions.VideoTextureQuality;
                    QualitySettings.vSyncCount = applicationOptions.VideoVSyncCount;
                    QualitySettings.antiAliasing = applicationOptions.VideoAntiAliasing;
                }
                else
                {
                    QualitySettings.SetQualityLevel(applicationOptions.VideoQualityLevel);
                }
#endif

                // Set the initial Audio options based on Cmune Prefs
                SfxManager.EnableAudio(ApplicationOptions.AudioEnabled);
                SfxManager.UpdateMasterVolume();
                SfxManager.UpdateMusicVolume();
                SfxManager.UpdateEffectsVolume();

                // Update the Key Mappings
                InputManager.Instance.ReadAllKeyMappings();

                // Run the perf test, if the machine is too low spec, we disable effects
                PerformanceTest.RunPerformanceCheck();

                //Load the spaceship level (We use the baseUrl for content, so level loading must come last)
                yield return Application.LoadLevelAdditiveAsync("LevelSpaceship");
                UberstrikeMap map = LevelManager.Instance.GetMapWithId(0);
                if (map.IsLoaded) GameState.SetCurrentSpace(map.Space);

                // Log the load time to Google Analytics
                //GoogleAnalytics.Instance.LogEvent("app-main-load", "Main Scene", true);

                StartCoroutine(BeginAuthenticateApplication());
            }
        }

#if UNITY_STANDALONE_OSX
        //Setup In-App Purchases for Mac App Store build
        InitializeStoreKit();
#elif UNITY_IPHONE
        InitializeiOSStoreKit();
#endif
    }

    private IEnumerator BeginAuthenticateApplication()
    {
        if (!PopupSystem.IsAnyPopupOpen)
        {
            initApplicationProgressPopup = PopupSystem.ShowProgress("Initializing", "Connecting to UberStrike...");
            initApplicationProgressPopup.ManualProgress = 0.3f;
        }

        // Authenticate the Application against the server
        yield return ApplicationWebServiceClient.AuthenticateApplication(
            ApplicationDataManager.VersionLong,
            ApplicationDataManager.Channel,
            string.Empty,
            (callback) => OnAuthenticateApplication(callback),
            (exception) => OnAuthenticateApplicationException(exception));

        PopupSystem.HideMessage(initApplicationProgressPopup);
    }

    protected override void OnShutdown()
    {
        // If we are a standalone build, we get out of fullscreen on quit, so that it won't try to start fullscreen when we next open the app
        PlayerPrefs.SetInt("Screenmanager Is Fullscreen mode", 0);
    }

    private void OnApplicationFocus(bool isFocused)
    {
        if (isFocused)
        {
            Application.targetFrameRate = (applicationOptions != null) ? applicationOptions.GeneralTargetFrameRate : 200;
        }
        else
        {
            Application.targetFrameRate = 20;
        }
    }

    private void SetBaseStandaloneMapsURL()
    {
        // Set the default folder to check for bundled maps in the standalone 
        switch (Application.platform)
        {
            case RuntimePlatform.OSXPlayer:
                BaseStandaloneMapsURL = Application.dataPath + "/Data/";
                break;
            case RuntimePlatform.WindowsPlayer:
                BaseStandaloneMapsURL = Application.dataPath + "/";
                break;
            default:
                BaseStandaloneMapsURL = string.Empty;
                break;
        }
    }

    private string GetConfigurationXmlFilePath()
    {
        if (Application.isEditor)
        {
            return ("file://" + Application.dataPath + "/../EditorConfiguration.xml");
        }
        switch (Application.platform)
        {
            case RuntimePlatform.WebGLPlayer:
                return Application.absoluteURL.Replace(".unity3d", ".xml");
            case RuntimePlatform.WindowsPlayer:
                return ("file://" + Application.dataPath + "/" + ApplicationDataManager.StandaloneFilename + ".xml");
            case RuntimePlatform.OSXPlayer:
                return ("file://" + Application.dataPath + "/Data/" + ApplicationDataManager.StandaloneFilename + ".xml");
            default:
                Debug.LogError("Cannot load Configuration Xml, " + ApplicationDataManager.Channel.ToString() + " ChannelType is not supported.");
                return string.Empty;
        }
    }

    private IEnumerator LoadConfigurationXml(string configurationXmlFilePath)
    {
        if ((string.IsNullOrEmpty(configurationXmlFilePath) || !configurationXmlFilePath.ToLower().Contains(".xml")) || (!configurationXmlFilePath.ToLower().Contains("file://") && !configurationXmlFilePath.ToLower().Contains("http://")))
        {
            Debug.LogError("Invalid url supplied for the Configuration XML file.\n'" + configurationXmlFilePath + "'");
            yield break;
        }


        WWW www = new WWW(configurationXmlFilePath);
        yield return www;

        if (!www.isDone || !string.IsNullOrEmpty(www.error))
        {
            CmuneDebug.LogError("WWW Error: " + www.error);
            yield break;
        }

        if (!string.IsNullOrEmpty(www.text))
        {
            clientConfigurationXml = www.text;
        }
        else
        {
            CmuneDebug.LogError("The Configuration Xml was null or empty.");
        }
    }

    private ClientConfiguration ParseConfigurationXml(string configurationXml)
    {
        ClientConfiguration clientConfiguration = new ClientConfiguration();
        try
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader(configurationXml));
            while (xmlReader.Read())
            {
                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.Name == "Application"))
                {
                    if (xmlReader.MoveToAttribute("BuildType"))
                        clientConfiguration.BuildString = xmlReader.Value;
                    if (xmlReader.MoveToAttribute("DebugLevel"))
                        clientConfiguration.DebugLevelString = xmlReader.Value;
                    if (xmlReader.MoveToAttribute("Version"))
                        clientConfiguration.Version = xmlReader.Value;
                    if (xmlReader.MoveToAttribute("WebServiceBaseUrl"))
                        clientConfiguration.WebServiceBaseUrl = xmlReader.Value;
                    if (xmlReader.MoveToAttribute("ContentBaseUrl"))
                        clientConfiguration.ContentBaseUrl = xmlReader.Value;
                    if (xmlReader.MoveToAttribute("ChannelType"))
                        clientConfiguration.ChannelTypeString = xmlReader.Value;
                    if (xmlReader.MoveToAttribute("LocalWebplayer"))
                        clientConfiguration.IsLocalWebplayerString = xmlReader.Value;
                }
            }

            if (!clientConfiguration.IsValid())
            {
                throw new Exception("Missing critical elements and/or attributes.");
            }
        }
        catch (Exception ex)
        {
            CmuneDebug.LogError("The Configuration XML was malformed.\n" + ex.Message);
            return null;
        }

        // Debug out the ClientConfiguration ChannelType only if we are not in the WebPlayer (as ChannelType comes from Application.srcValue on the web)
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            CmuneDebug.Log(string.Format("Parsed Client Configuration Xml:\\nBuild='{0}'\\nVersion='{1}'\\nWebSerivcesBaseUrl='{2}'\\nContentBaseUrl='{3}'", clientConfiguration.BuildType, clientConfiguration.Version, clientConfiguration.WebServiceBaseUrl, clientConfiguration.ContentBaseUrl));
        else
            CmuneDebug.Log(string.Format("Parsed Client Configuration Xml:\\nBuild='{0}'\\nVersion='{1}'\\nWebSerivcesBaseUrl='{2}'\\nContentBaseUrl='{3}'\\nChannelType={4}", clientConfiguration.BuildType, clientConfiguration.Version, clientConfiguration.WebServiceBaseUrl, clientConfiguration.ContentBaseUrl, clientConfiguration.ChannelType));

        return clientConfiguration;
    }

    private void HandleApplicationAuthenticationError(string message)
    {
        PopupSystem.HideMessage(initApplicationProgressPopup);

        switch (Channel)
        {
            case ChannelType.MacAppStore:
                PopupSystem.ShowMessage(LocalizedStrings.Error, message, PopupSystem.AlertType.OK, Application.Quit);
                break;
            case ChannelType.WebPortal:
            case ChannelType.WebFacebook:
            case ChannelType.Kongregate:
                PopupSystem.ShowMessage(LocalizedStrings.Error, message, PopupSystem.AlertType.None);
                break;
            case ChannelType.OSXStandalone:
            case ChannelType.WindowsStandalone:
                PopupSystem.ShowMessage(LocalizedStrings.Error, message, PopupSystem.AlertType.OK, RetryAuthentiateApplication);
                break;
            case ChannelType.Android:
            case ChannelType.IPhone:
            case ChannelType.IPad:
                PopupSystem.ShowMessage(LocalizedStrings.Error, message, PopupSystem.AlertType.OK, RetryAuthentiateApplication);
                break;
            default:
                PopupSystem.ShowMessage(LocalizedStrings.Error, message + "This client type is not supported.", PopupSystem.AlertType.OK, Application.Quit);
                break;
        }
    }

    private void HandleConfigurationMissingError(string message)
    {
        PopupSystem.HideMessage(initApplicationProgressPopup);

        switch (Channel)
        {
            case ChannelType.MacAppStore:
                PopupSystem.ShowMessage(LocalizedStrings.Error, message, PopupSystem.AlertType.OK, Application.Quit);
                break;
            case ChannelType.WebPortal:
            case ChannelType.WebFacebook:
            case ChannelType.Kongregate:
                PopupSystem.ShowMessage(LocalizedStrings.Error, message, PopupSystem.AlertType.None);
                break;
            case ChannelType.OSXStandalone:
            case ChannelType.WindowsStandalone:
                PopupSystem.ShowMessage(LocalizedStrings.Error, message, PopupSystem.AlertType.OK, Application.Quit);
                break;
            case ChannelType.Android:
            case ChannelType.IPhone:
            case ChannelType.IPad:
                PopupSystem.ShowMessage(LocalizedStrings.Error, message, PopupSystem.AlertType.OK, Application.Quit);
                break;
            default:
                PopupSystem.ShowMessage(LocalizedStrings.Error, message + "This client type is not supported.", PopupSystem.AlertType.OK, Application.Quit);
                break;
        }
    }

    private void RetryAuthentiateApplication()
    {
        StartCoroutine(BeginAuthenticateApplication());
    }

    private void OnAuthenticateApplicationException(Exception exception)
    {
        CmuneDebug.LogError("An exception occurred while authenticating the application: " + exception.Message);

        //string message = (exception.Message.Contains("Could not resolve host:") || exception.Message.Contains("Failed downloading http://")) ? "There was an error connecting to the server.\nPlease check your Internet connection." : "There was an error authenticating UberStrike with the server.";
        HandleApplicationAuthenticationError("There was a problem loading UberStrike. Please check your internet connection and try again.");
    }

    private void OnAuthenticateApplication(AuthenticateApplicationView ev)
    {
        if ((ev != null) && ev.IsEnabled)
        {
            // Setup the data for Rinjdael encryption
            Configuration.EncryptionInitVector = ev.EncryptionInitVector;
            Configuration.EncryptionPassPhrase = ev.EncryptionPassPhrase;

            // We have authenticated the application and the player and are considered online (this is important for calling encrypted web services like RecordException)
            ApplicationDataManager.IsOnline = true;

            // Setup Game Servers
            if (CmuneNetworkConfiguration.Instance.CustomGameServer.IsEnabled)
            {
                // Setup Local Game Server only if we are in the editor
                Singleton<GameServerManager>.Instance.AddGameServer(new PhotonView()
                {
                    IP = CmuneNetworkConfiguration.Instance.CustomGameServer.Ip,
                    Port = CmuneNetworkConfiguration.Instance.CustomGameServer.Port,
                    UsageType = PhotonUsageType.All,
                    Name = string.Format("Custom Game Server ({0})", CmuneNetworkConfiguration.Instance.CustomGameServer.Address),
                    Region = RegionType.AsiaPacific
                });
            }
            else
            {
                foreach (PhotonView v in ev.GameServers)
                {
#if !UNITY_ANDROID && !UNITY_IPHONE
                    // mobile servers should only be seen by mobile devices
                    if (v.UsageType == PhotonUsageType.Mobile) continue;
#endif
                    GameServerManager.Instance.AddGameServer(v);
                }
            }

            // Setup Comm Server
            if (CmuneNetworkConfiguration.Instance.CustomCommServer.IsEnabled)
            {
                CmuneNetworkManager.CurrentCommServer = new GameServerView(new PhotonView()
                {
                    IP = CmuneNetworkConfiguration.Instance.CustomCommServer.Ip,
                    Port = CmuneNetworkConfiguration.Instance.CustomCommServer.Port,
                    UsageType = PhotonUsageType.CommServer,
                    Name = string.Format("Custom Comm Server ({0})", CmuneNetworkConfiguration.Instance.CustomCommServer.Address),
                    Region = RegionType.AsiaPacific
                });
            }
            else
            {
                CmuneNetworkManager.CurrentCommServer = new GameServerView(ev.CommServer);
            }

#if UNITY_ANDROID || UNITY_IPHONE
            _showLoadingBackground = false;
#endif

            // If the client is out of date but still usable, we warn the player
            if (ev.WarnPlayer)
            {
                HandleVersionWarning();
            }
            else
            {
                AuthenticationManager.Instance.LoginByChannel();
            }
        }
        else
        {
            CmuneDebug.LogError("Your version is out of date and cannot be used.");
            HandleVersionError();
        }
    }

    private void HandleVersionWarning()
    {
        switch (Channel)
        {
            case ChannelType.MacAppStore:
                PopupSystem.ShowMessage("Warning", "Your UberStrike client is out of date. Click OK to update from the Mac App Store.", PopupSystem.AlertType.OKCancel, OpenMacAppStoreUpdatesPage, AuthenticationManager.Instance.LoginByChannel);
                break;
            case ChannelType.WebPortal:
            case ChannelType.WebFacebook:
            case ChannelType.Kongregate:
                PopupSystem.ShowMessage("Warning", "Your UberStrike client is out of date. You should refresh your browser.", PopupSystem.AlertType.OK, AuthenticationManager.Instance.LoginByChannel);
                break;
            case ChannelType.OSXStandalone:
            case ChannelType.WindowsStandalone:
            case ChannelType.Android:
            case ChannelType.IPhone:
            case ChannelType.IPad:
                PopupSystem.ShowMessage("Warning", "Your UberStrike client is out of date. Click OK to update from our website.", PopupSystem.AlertType.OKCancel, OpenUberStrikeUpdatePage, AuthenticationManager.Instance.LoginByChannel);
                break;
            default:
                PopupSystem.ShowMessage(LocalizedStrings.Error, "Your UberStrike client is not supported. Please update from our website.\n(Invalid Channel: " + Channel + ")", PopupSystem.AlertType.OK, OpenUberStrikeUpdatePage);
                break;
        }
    }

    private void HandleVersionError()
    {
        switch (Channel)
        {
            case ChannelType.MacAppStore:
                PopupSystem.ShowMessage(LocalizedStrings.Error, "Your UberStrike client is out of date. Please update from the Mac App Store.", PopupSystem.AlertType.OK, OpenMacAppStoreUpdatesPage);
                break;
            case ChannelType.WebPortal:
            case ChannelType.WebFacebook:
            case ChannelType.Kongregate:
                PopupSystem.ShowMessage(LocalizedStrings.Error, "Your UberStrike client is out of date. Please refresh your browser.", PopupSystem.AlertType.None);
                break;
            case ChannelType.OSXStandalone:
            case ChannelType.WindowsStandalone:
            case ChannelType.Android:
            case ChannelType.IPhone:
            case ChannelType.IPad:
                PopupSystem.ShowMessage(LocalizedStrings.Error, "Your UberStrike client is out of date. Please update from our website.", PopupSystem.AlertType.OK, OpenUberStrikeUpdatePage);
                break;
            default:
                PopupSystem.ShowMessage(LocalizedStrings.Error, "Your UberStrike client is not supported. Please update from our website.\n(Invalid Channel: " + Channel + ")", PopupSystem.AlertType.OK, OpenUberStrikeUpdatePage);
                break;
        }
    }

    private void OpenMacAppStoreUpdatesPage()
    {
        OpenUrl(string.Empty, "macappstore://showUpdatesPage");
        Application.Quit();
    }

    private void OpenUberStrikeUpdatePage()
    {
        OpenUrl(string.Empty, "http://uberstrike.com");
        Application.Quit();
    }

    public void LockApplication(string message = "An error occured that forced UberStrike to halt.")
    {
        PopupSystem.ClearAll();

        switch (Channel)
        {
            case ChannelType.MacAppStore:
                PopupSystem.ShowMessage(LocalizedStrings.Error, message, PopupSystem.AlertType.OK, Application.Quit);
                break;
            case ChannelType.WebPortal:
            case ChannelType.WebFacebook:
            case ChannelType.Kongregate:
                PopupSystem.ShowMessage(LocalizedStrings.Error, message, PopupSystem.AlertType.None);
                break;
            case ChannelType.OSXStandalone:
            case ChannelType.WindowsStandalone:
                PopupSystem.ShowMessage(LocalizedStrings.Error, message, PopupSystem.AlertType.OK, Logout);
                break;
            case ChannelType.Android:
            case ChannelType.IPhone:
            case ChannelType.IPad:
                PopupSystem.ShowMessage(LocalizedStrings.Error, message, PopupSystem.AlertType.OK, Logout);
                break;
        }
    }

    private void SetVersionLong(ClientConfiguration clientConfiguration)
    {
        if (Application.isEditor)
        {
            VersionLong = clientConfiguration.Version;
        }
        else
        {
            switch (ApplicationDataManager.Channel)
            {
                case ChannelType.WebPortal:
                case ChannelType.WebFacebook:
                case ChannelType.Kongregate:
                    {
                        string webPlayerUrl = Application.absoluteURL;
                        int fileIndex = webPlayerUrl.IndexOf(".unity3d?");
                        if (fileIndex > 0)
                        {
                            webPlayerUrl = webPlayerUrl.Remove(fileIndex);
                        }
                        int slashIndex = webPlayerUrl.LastIndexOf('/');
                        if (slashIndex > 0)
                        {
                            webPlayerUrl = webPlayerUrl.Substring(slashIndex);
                        }
                        int dashIndex = webPlayerUrl.LastIndexOf('.');
                        if (dashIndex > 0)
                        {
                            VersionLong = webPlayerUrl.Substring(dashIndex + 1);
                        }
                        else
                        {
                            VersionLong = clientConfiguration.Version;
                        }
                        return;
                    }
                case ChannelType.WindowsStandalone:
                case ChannelType.OSXStandalone:
                    VersionLong = clientConfiguration.Version;
                    return;
            }
            VersionLong = clientConfiguration.Version;
        }
    }

    public void Logout()
    {
        if (GameState.HasCurrentGame)
        {
            GameStateController.Instance.UnloadGameMode();
        }
        // remove avatar
        if (GameState.LocalDecorator != null)
            AvatarBuilder.Destroy(GameState.LocalDecorator.gameObject);
        GameState.LocalDecorator = null;

        // reset singletons
        PlayerDataManager.Instance.Dispose();
        InventoryManager.Instance.Dispose();
        LoadoutManager.Instance.Dispose();
        ClanDataManager.Instance.Dispose();
        ChatManager.Instance.Dispose();
        InboxManager.Instance.Dispose();
        TransactionHistory.Instance.Dispose();
        GlobalUIRibbon.Instance.Hide();

        // clean up inbox
        InboxThread.Current = null;

        CmuneEventHandler.Route(new LogoutEvent());

        // go back to login screen
        MenuPageManager.Instance.UnloadCurrentPage();
        AuthenticationManager.Instance.LoginByChannel();
        CommConnectionManager.Stop();
    }
    /// <summary>
    /// Called from Web Page
    /// </summary>
    public void RefreshWallet()
    {
        StartCoroutine(StartRefreshWalletInventory());
    }

    public void SetBrowserInfo(string result)
    {
        bool hadError = true;

        if (result != null)
        {
            string[] browserInfo = result.Split('%');
            if (browserInfo != null)
            {
                localSystemInfo.BrowserIdentifier = (browserInfo.Length > 0 && browserInfo[0] != null) ? browserInfo[0] : "Error getting data.";
                localSystemInfo.BrowserVersion = (browserInfo.Length > 1 && browserInfo[1] != null) ? browserInfo[1] : "Error getting data.";
                localSystemInfo.BrowserMajorVersion = (browserInfo.Length > 2 && browserInfo[2] != null) ? browserInfo[2] : "Error getting data.";
                localSystemInfo.BrowserMinorVersion = (browserInfo.Length > 3 && browserInfo[3] != null) ? browserInfo[3] : "Error getting data.";
                localSystemInfo.BrowserEngine = (browserInfo.Length > 4 && browserInfo[4] != null) ? browserInfo[4] : "Error getting data.";
                localSystemInfo.BrowserEngineVersion = (browserInfo.Length > 5 && browserInfo[5] != null) ? browserInfo[5] : "Error getting data.";
                localSystemInfo.BrowserUserAgent = (browserInfo.Length > 6 && browserInfo[6] != null) ? browserInfo[6] : "Error getting data.";
                hadError = false;
            }
        }

        if (hadError)
        {
            localSystemInfo.BrowserIdentifier =
            localSystemInfo.BrowserVersion =
            localSystemInfo.BrowserMajorVersion =
            localSystemInfo.BrowserMinorVersion =
            localSystemInfo.BrowserEngine =
            localSystemInfo.BrowserEngineVersion =
            localSystemInfo.BrowserUserAgent = "Error communicating with browser.";
        }
    }

    public static void OpenUrl(string title, string url)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Application.ExternalCall("displayMessage", title, url);
        }
        else
        {
            if (Screen.fullScreen && Application.platform != RuntimePlatform.WindowsPlayer)
            {
                ScreenResolutionManager.IsFullScreen = false;
            }
            Application.OpenURL(url);
        }
    }

    public void OpenBuyCredits()
    {
        switch (Channel)
        {
            case ChannelType.WebPortal:
            case ChannelType.WebFacebook:
            case ChannelType.Kongregate:
                ScreenResolutionManager.IsFullScreen = false;
                Application.ExternalCall("getCreditsWrapper", PlayerDataManager.CmidSecure);
                break;
            case ChannelType.OSXStandalone:
            case ChannelType.WindowsStandalone:
                OpenUrl(string.Empty, GetStandalonePaymentUrl());
                StartCoroutine(ShowStandaloneRefreshBalancePopup(2.0f));
                break;
            case ChannelType.IPad:
            case ChannelType.IPhone:
                if (MasBundleManager.Instance.CanMakeMasPayments)
                {
                    MenuPageManager.Instance.LoadPage(PageType.Shop, false);
                    CmuneEventHandler.Route(new SelectShopAreaEvent() { ShopArea = ShopArea.Credits });
                }
                else
                {
                    PopupSystem.ShowMessage(LocalizedStrings.Error, "Sorry, In App Purchases are currently unavailable.", PopupSystem.AlertType.OK);
                }
                break;
            case ChannelType.MacAppStore:
                if (MasBundleManager.Instance.CanMakeMasPayments)
                {
                    MenuPageManager.Instance.LoadPage(PageType.Shop, false);
                    CmuneEventHandler.Route(new SelectShopAreaEvent() { ShopArea = ShopArea.Credits });
                }
                else
                    PopupSystem.ShowMessage(LocalizedStrings.Error, "Sorry, In App Purchases are only available in Mac OSX Lion (v 10.7) and above.", PopupSystem.AlertType.OK);
                break;
            default:
                CmuneDebug.LogError("OpenBuyCredits not supported on channel: " + Channel);
                break;
        }
    }

    public void OpenLinkFacebookUrl()
    {
        OpenUrl(string.Empty, GetLinkFacebookUrl());
    }

    private IEnumerator ShowStandaloneRefreshBalancePopup(float delayInSecs)
    {
        yield return new WaitForSeconds(delayInSecs);
        PopupSystem.ShowMessage("Buy Credits", "Did you make a purchase? If so, click Update to refresh your point and credit balance.", PopupSystem.AlertType.OKCancel, "UPDATE", RefreshWallet);
    }

    private string GetLinkFacebookUrl()
    {
        string url = string.Empty;

        // Get the base domain depending on the BuildType
        switch (BuildType)
        {
            case BuildType.Dev: url = "http://dev.uberstrike.com/"; break;
            case BuildType.Staging: url = "http://qa.uberstrike.com/"; break;
            case BuildType.Prod: url = "http://uberstrike.com/"; break;
        }

        // Fill in the rest of the data
        url += string.Format("account/facebooklogin?channel={0}", (int)Channel);

        return url;
    }

    private string GetStandalonePaymentUrl()
    {
        string url = string.Empty;

        // Get the base domain depending on the BuildType
        switch (BuildType)
        {
            case BuildType.Dev: url = "http://dev.uberstrike.com/"; break;
            case BuildType.Staging: url = "http://qa.uberstrike.com/"; break;
            case BuildType.Prod: url = "http://uberstrike.com/"; break;
        }

        // Fill in the rest of the data
        url += string.Format("account/externallogin?channel={0}&email={1}", (int)Channel, PlayerDataManager.EmailSecure);

        return url;
    }

    private string GetStandaloneUpdateUrl()
    {
        string url = string.Empty;

        // Get the base domain depending on the BuildType
        switch (BuildType)
        {
            case BuildType.Dev: url = "http://dev.uberstrike.com/"; break;
            case BuildType.Staging: url = "http://qa.uberstrike.com/"; break;
            case BuildType.Prod: url = "http://uberstrike.com/"; break;
        }

        // Fill in the rest of the data
        url += string.Format("download?mode=update&channel={0}", (int)Channel);

        return url;
    }

    public void ShowFBInviteFriendsLightbox()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Application.ExternalCall("showFBInviteFriendsLightbox");
        }
    }

    public void PublishFBLevelUpStreamPost(string level)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Application.ExternalCall("publishFBStreamPost", "levelup", level);
            PopupSystem.ShowMessage("Publish to Facebook", "Your Level Up was successfully published!", PopupSystem.AlertType.OK);
        }
    }

    public void ShowMenuTabsInBrowser()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Application.ExternalCall("displayHeader", PlayerDataManager.CmidSecure.ToString());
        }
    }

    public void SetAccountCreatedStep()
    {
        // Write the step tracking info (Account Created)
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Application.ExternalCall("wsTracking", ((int)UserInstallStepType.AccountCreated).ToString());
        }
    }

    public void ShowAttributedItems(Dictionary<int, int> itemsWithDate)
    {
        List<IUnityItem> items = new List<IUnityItem>();

        foreach (var id in itemsWithDate.Keys)
            items.Add(ItemManager.Instance.GetItemInShop(id));

        if (items.Count > 0)
            PopupSystem.ShowItems("You've got new items!", "You've just been given some new items to get started.\nClick below to go to the Shop and equip them!", items);
    }

#if UNITY_STANDALONE_OSX
    private void InitializeStoreKit()
    {
        if (Channel == ChannelType.MacAppStore && !Application.isEditor)
        {
            try
            {
                StartCoroutine(StoreKitMacManager.Instance.StartEventListener());
            }
            catch
            {
                CmuneDebug.LogError("Couldn't Start the StoreKitMacManager. The EventListener and Manager GameObjects are most likely missing from the GameObject Hierarchy.");
            }
        }
    }
#endif

#if UNITY_IPHONE
    private void InitializeiOSStoreKit()
    {
        if ((Channel == ChannelType.IPad || Channel == ChannelType.IPhone))
        {
            try
            {
                GameObject storeKit = new GameObject("StoreKitManager");
                storeKit.AddComponent(typeof(StoreKitManager));
            }
            catch
            {
                CmuneDebug.LogError("Couldn't Start the StoreKitManager. The EventListener and Manager GameObjects are most likely missing from the GameObject Hierarchy.");
            }
        }
    }
#endif

    public static string FrameRate
    {
        get
        {
            int msPerFrame = Mathf.RoundToInt(Time.smoothDeltaTime * 1000);
            return string.Format("{0} ({1}ms)", 1000 / msPerFrame, msPerFrame);
        }
    }

    private void SetChannel(ClientConfiguration clientConfiguration)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer && WebPlayerSrcValues.IsValid)
        {
            // If we are running the web player, get the channel from the Application.srcValue URL
            Channel = WebPlayerSrcValues.ChannelType;
        }
        else
        {
            // We are in the standalone or editor, just read the channel from ClientConfiguration
            Channel = clientConfiguration.ChannelType;
        }
    }

    private IEnumerator StartRefreshWalletInventory()
    {
        yield return StartCoroutine(PlayerDataManager.Instance.StartGetMemberWallet(true));
        yield return StartCoroutine(ItemManager.Instance.StartGetInventory(true));
    }

    public void SetLevelCapsView(List<PlayerLevelCapView> caps)
    {
        XpByLevel.Clear();
        foreach (var xp in caps)
        {
            XpByLevel[xp.Level] = xp.XPRequired;
        }
    }

    public bool LoadMapsUsingWebService
    {
        get { return loadMapsUsingWebService; }
    }

    public static ApplicationOptions ApplicationOptions
    {
        get { return applicationOptions; }
    }

    public CmuneSystemInfo LocalSystemInfo
    {
        get { return localSystemInfo; }
    }

    public static DateTime ServerDateTime
    {
        get
        {
            return serverDateTime.AddSeconds((double)(Time.time - applicationDateTime));
        }
        set
        {
            serverDateTime = value;
            applicationDateTime = Time.realtimeSinceStartup;
        }
    }

    #region JavaScript

    private const string _evalBrwJS =
        @"
        var br=new Array(4);
        //var os=new Array(2);
        //var flash=new Array(2);
        br=getBrowser();
        //os=getOS();
        //flash=hasFlashPlugin();
        //popups = popupsAllowed();
        //jsver = jsVersion();
        //document.write('Browser identifier: '+br[0]+'<br />');
        //document.write('Browser version: '+br[1]+'<br />');
        //document.write('Browser major version: '+getMajorVersion(br[1])+'<br />');
        //document.write('Browser minor version: '+getMinorVersion(br[1])+'<br />');
        //document.write('Browser engine: '+br[2]+'<br />');
        //document.write('Browser engine version: '+br[3]+'<br />');
        //document.write('Full user agent string: '+getFullUAString()+'<br />');
        //document.write('Operating system identifier: '+os[0]+'<br />');
        //document.write('Operating system version: '+os[1]+'<br />');
        //document.write('Is Flash installed? '+ (flash[0]==2 ? 'Yes' : (flash[0] == 1 ? 'No' : 'unknown'))+'<br />');
        //document.write('Flash version: '+flash[1] + '<br />');
        //document.write('Are popups allowed for this site? ' + (popups ? 'Yes' : 'No') + '<br />' );
        //document.write('What is the newest version of Javascript this browser supports? ' + jsver + '<br />');
        GetUnity().SendMessage('ApplicationDataManager', 'SetBrowserInfo', 
        br[0] + '%' + 
        br[1] + '%' +
        getMajorVersion(br[1]) + '%' +
        getMinorVersion(br[1]) + '%' + 
        br[2] + '%' +
        br[3] + '%' +
        getFullUAString()
        ); 
        ";

    #endregion

}
