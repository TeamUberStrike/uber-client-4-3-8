using UnityEngine;
using System.Text;

public class CmuneSystemInfo
{
    public string OperatingSystem;
    public string ProcessorType;
    public string ProcessorCount;
    public string SystemMemorySize;
    public string GraphicsDeviceName;
    public string GraphicsDeviceVendor;
    public string GraphicsDeviceVersion;
    public string GraphicsMemorySize;
    public string GraphicsShaderLevel;
    public string GraphicsPixelFillRate;
    public string SupportsImageEffects;
    public string SupportsRenderTextures;
    public string SupportsShadows;
    public string SupportsVertexPrograms;

    public string Platform;
    public string RunInBackground;
    public string AbsoluteURL;
    public string DataPath;
    public string BackgroundLoadingPriority;
    public string SrcValue;
    public string SystemLanguage;
    public string TargetFrameRate;
    public string UnityVersion;

    public string Gravity;
    public string BounceThreshold;
    public string MaxAngularVelocity;
    public string MinPenetrationForPenalty;
    public string PenetrationPenaltyForce;
    public string SleepAngularVelocity;
    public string SleepVelocity;
    public string SolverIterationCount;

    public string CurrentResolution;
    public string AmbientLight;
    public string FlareStrength;
    public string FogEnabled;
    public string FogColor;
    public string FogDensity;
    public string HaloStrength;

    public string CurrentQualityLevel;
    public string AnisotropicFiltering;
    public string MasterTextureLimit;
    public string MaxQueuedFrames;
    public string PixelLightCount;
    public string ShadowCascades;
    public string ShadowDistance;
    public string SoftVegetationEnabled;

    public string BrowserIdentifier;
    public string BrowserVersion;
    public string BrowserMajorVersion;
    public string BrowserMinorVersion;
    public string BrowserEngine;
    public string BrowserEngineVersion;
    public string BrowserUserAgent;

    public CmuneSystemInfo()
    {
        // Unity System Info
        OperatingSystem = SystemInfo.operatingSystem;
        ProcessorType = SystemInfo.processorType;
        ProcessorCount = SystemInfo.processorCount.ToString("N0");
        SystemMemorySize = SystemInfo.systemMemorySize.ToString("N0") + "Mb";
        GraphicsDeviceName = SystemInfo.graphicsDeviceName;
        GraphicsDeviceVendor = SystemInfo.graphicsDeviceVendor;
        GraphicsDeviceVersion = SystemInfo.graphicsDeviceVersion;
        GraphicsMemorySize = SystemInfo.graphicsMemorySize.ToString("N0") + "Mb";
        GraphicsShaderLevel = GetShaderLevelName(SystemInfo.graphicsShaderLevel);
        GraphicsPixelFillRate = SystemInfo.graphicsPixelFillrate.ToString();
        SupportsImageEffects = SystemInfo.supportsImageEffects.ToString();
        SupportsRenderTextures = SystemInfo.supportsRenderTextures.ToString();
        SupportsShadows = SystemInfo.supportsShadows.ToString();
        SupportsVertexPrograms = SystemInfo.supportsVertexPrograms.ToString();

        // Unity Application Info
        Platform = Application.platform.ToString();
        RunInBackground = Application.runInBackground.ToString();
        AbsoluteURL = Application.absoluteURL;
        DataPath = Application.dataPath;
        BackgroundLoadingPriority = Application.backgroundLoadingPriority.ToString();
        SrcValue = Application.srcValue;
        SystemLanguage = Application.systemLanguage.ToString();
        TargetFrameRate = Application.targetFrameRate.ToString("N0");
        UnityVersion = Application.unityVersion;

        // Unity Physics Info
        Gravity = Physics.gravity.ToString();
        BounceThreshold = Physics.bounceThreshold.ToString("N2");
        MaxAngularVelocity = Physics.maxAngularVelocity.ToString("N2");
        MinPenetrationForPenalty = Physics.minPenetrationForPenalty.ToString("N2");
        PenetrationPenaltyForce = Physics.penetrationPenaltyForce.ToString("N2");
        SleepAngularVelocity = Physics.sleepAngularVelocity.ToString("N2");
        SleepVelocity = Physics.sleepVelocity.ToString("N2");
        SolverIterationCount = Physics.defaultSolverIterations.ToString("N2");

        // Unity Rendering Info
        CurrentResolution = "X " + Screen.width.ToString() + ", Y " + Screen.height.ToString() + ", Refresh " + Screen.currentResolution.refreshRate.ToString("N0") + "Hz";
        AmbientLight = RenderSettings.ambientLight.ToString();
        FlareStrength = RenderSettings.flareStrength.ToString("N2");
        FogEnabled = RenderSettings.fog.ToString();
        FogColor = RenderSettings.fogColor.ToString();
        FogDensity = RenderSettings.fogDensity.ToString("N2");
        HaloStrength = RenderSettings.haloStrength.ToString("N2");

        // Unity Quality Settings Info
        CurrentQualityLevel = QualitySettings.GetQualityLevel().ToString();
        AnisotropicFiltering = QualitySettings.anisotropicFiltering.ToString();
        MasterTextureLimit = QualitySettings.globalTextureMipmapLimit.ToString();
        MaxQueuedFrames = QualitySettings.maxQueuedFrames.ToString();
        PixelLightCount = QualitySettings.pixelLightCount.ToString();
        ShadowCascades = QualitySettings.shadowCascades.ToString();
        ShadowDistance = QualitySettings.shadowDistance.ToString("N2");
        SoftVegetationEnabled = QualitySettings.softVegetation.ToString();

        BrowserIdentifier =
        BrowserVersion =
        BrowserMajorVersion =
        BrowserMinorVersion =
        BrowserEngine =
        BrowserEngineVersion =
        BrowserUserAgent = "No information.";
    }

    private string GetShaderLevelName(int shaderLevel)
    {
        switch (shaderLevel)
        {
            case 30:
                return "Shader Model 3.0 - We like!";
            case 20:
                return "Shader Model 2.x";
            case 10:
                return "Shader Model 1.x";
            case 7:
                return "Fixed function, DirectX 7.";
            case 6:
                return "Fixed function, DirectX 6.";
            case 5:
                return "Fixed function, DirectX 5.";
            default:
                return "Unknown";
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("///SYSTEM INFO REPORT///");
        sb.AppendLine(string.Empty);
        sb.AppendLine("UNITY SYSTEM INFO");
        sb.AppendLine("   Operating System: " + OperatingSystem);
        sb.AppendLine("   ProcessorType: " + ProcessorType);
        sb.AppendLine("   ProcessorCount: " + ProcessorCount);
        sb.AppendLine("   SystemMemorySize: " + SystemMemorySize);
        sb.AppendLine("   GraphicsDeviceName: " + GraphicsDeviceName);
        sb.AppendLine("   GraphicsDeviceVendor: " + GraphicsDeviceVendor);
        sb.AppendLine("   GraphicsDeviceVersion: " + GraphicsDeviceVersion);
        sb.AppendLine("   GraphicsMemorySize: " + GraphicsMemorySize);
        sb.AppendLine("   GraphicsShaderLevel: " + GraphicsShaderLevel);
        sb.AppendLine("   GraphicsPixelFillRate: " + GraphicsPixelFillRate);
        sb.AppendLine("   SupportsImageEffects: " + SupportsImageEffects);
        sb.AppendLine("   SupportsRenderTextures: " + SupportsRenderTextures);
        sb.AppendLine("   SupportsShadows: " + SupportsShadows);
        sb.AppendLine("   SupportsVertexPrograms: " + SupportsVertexPrograms);
        sb.AppendLine(string.Empty);
        sb.AppendLine("UNITY APPLICATION INFO");
        sb.AppendLine("   Platform: " + Platform);
        sb.AppendLine("   Run In Background: " + RunInBackground);
        sb.AppendLine("   Absolute URL: " + AbsoluteURL);
        sb.AppendLine("   Data Path: " + DataPath);
        sb.AppendLine("   Background Loading Priority: " + BackgroundLoadingPriority);
        sb.AppendLine("   Src Value: " + SrcValue);
        sb.AppendLine("   System Language: " + SystemLanguage);
        sb.AppendLine("   Target Frame Rate: " + TargetFrameRate);
        sb.AppendLine("   Unity Version: " + UnityVersion);
        sb.AppendLine(string.Empty);
        sb.AppendLine("UNITY PHYSICS INFO");
        sb.AppendLine("   Gravity: " + Gravity);
        sb.AppendLine("   Bounce Threshold: " + BounceThreshold);
        sb.AppendLine("   Max Angular Velocity: " + MaxAngularVelocity);
        sb.AppendLine("   Min Penetration For Penalty: " + MinPenetrationForPenalty);
        sb.AppendLine("   Penetration Penalty Force: " + PenetrationPenaltyForce);
        sb.AppendLine("   Sleep Angular Velocity: " + SleepAngularVelocity);
        sb.AppendLine("   Sleep Velocity: " + SleepVelocity);
        sb.AppendLine("   Solver Iteration Count: " + SolverIterationCount);
        sb.AppendLine(string.Empty);
        sb.AppendLine("UNITY RENDERING INFO");
        sb.AppendLine("   Current Resolution: " + CurrentResolution);
        sb.AppendLine("   Ambient Light: " + AmbientLight);
        sb.AppendLine("   Flare Strength: " + FlareStrength);
        sb.AppendLine("   Fog Enabled: " + FogEnabled);
        sb.AppendLine("   Fog Color: " + FogColor);
        sb.AppendLine("   Fog Density: " + FogDensity);
        sb.AppendLine("   Halo Strength: " + HaloStrength);
        sb.AppendLine(string.Empty);
        sb.AppendLine("UNITY QUALITY SETTINGS INFO");
        sb.AppendLine("   Current Quality Level: " + CurrentQualityLevel);
        sb.AppendLine("   Anisotropic Filtering: " + AnisotropicFiltering);
        sb.AppendLine("   Master Texture Limit: " + MasterTextureLimit);
        sb.AppendLine("   Max Queued Frames: " + MaxQueuedFrames);
        sb.AppendLine("   Pixel Light Count: " + PixelLightCount);
        sb.AppendLine("   Shadow Cascades: " + ShadowCascades);
        sb.AppendLine("   Shadow Distance: " + ShadowDistance);
        sb.AppendLine("   Soft Vegetation Enabled: " + SoftVegetationEnabled);
        sb.AppendLine(string.Empty);
        sb.AppendLine("BROWSER INFO");
        sb.AppendLine("   Browser Identifier: " + BrowserIdentifier);
        sb.AppendLine("   Browser Version: " + BrowserVersion);
        sb.AppendLine("   Browser Major Version: " + BrowserMajorVersion);
        sb.AppendLine("   Browser Minor Version: " + BrowserMinorVersion);
        sb.AppendLine("   Browser Engine: " + BrowserEngine);
        sb.AppendLine("   Browser Engine Version: " + BrowserEngineVersion);
        sb.AppendLine("   Browser User Agent: " + BrowserUserAgent);
        sb.AppendLine("GAME SERVER INFO");
        if (GameServerManager.Instance.PhotonServerCount > 0)
        {
            foreach (var gs in GameServerManager.Instance.PhotonServerList)
                sb.AppendLine(string.Format("   Server:{0} Ping:{1}", gs.ConnectionString, gs.Latency));
        }
        else
        {
            sb.AppendLine("   No Game Server Information available.");
        }
        sb.AppendLine("END OF REPORT");
        return sb.ToString();
    }

    public string ToHTML()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<h2>SYSTEM INFO REPORT</h2>");
        sb.AppendLine("<h3>UNITY SYSTEM INFO</h3>");
        sb.AppendLine("        Operating System: " + OperatingSystem);
        sb.AppendLine("<br/>   ProcessorType: " + ProcessorType);
        sb.AppendLine("<br/>   ProcessorCount: " + ProcessorCount);
        sb.AppendLine("<br/>   SystemMemorySize: " + SystemMemorySize);
        sb.AppendLine("<br/>   GraphicsDeviceName: " + GraphicsDeviceName);
        sb.AppendLine("<br/>   GraphicsDeviceVendor: " + GraphicsDeviceVendor);
        sb.AppendLine("<br/>   GraphicsDeviceVersion: " + GraphicsDeviceVersion);
        sb.AppendLine("<br/>   GraphicsMemorySize: " + GraphicsMemorySize);
        sb.AppendLine("<br/>   GraphicsShaderLevel: " + GraphicsShaderLevel);
        sb.AppendLine("<br/>   GraphicsPixelFillRate: " + GraphicsPixelFillRate);
        sb.AppendLine("<br/>   SupportsImageEffects: " + SupportsImageEffects);
        sb.AppendLine("<br/>   SupportsRenderTextures: " + SupportsRenderTextures);
        sb.AppendLine("<br/>   SupportsShadows: " + SupportsShadows);
        sb.AppendLine("<br/>   SupportsVertexPrograms: " + SupportsVertexPrograms);

        sb.AppendLine("<br/><h3>UNITY APPLICATION INFO</h3>");
        sb.AppendLine("        Platform: " + Platform);
        sb.AppendLine("<br/>   Run In Background: " + RunInBackground);
        sb.AppendLine("<br/>   Absolute URL: " + AbsoluteURL);
        sb.AppendLine("<br/>   Data Path: " + DataPath);
        sb.AppendLine("<br/>   Background Loading Priority: " + BackgroundLoadingPriority);
        sb.AppendLine("<br/>   Src Value: " + SrcValue);
        sb.AppendLine("<br/>   System Language: " + SystemLanguage);
        sb.AppendLine("<br/>   Target Frame Rate: " + TargetFrameRate);
        sb.AppendLine("<br/>   Unity Version: " + UnityVersion);

        sb.AppendLine("<br/><h3>UNITY PHYSICS INFO</h3>");
        sb.AppendLine("        Gravity: " + Gravity);
        sb.AppendLine("<br/>   Bounce Threshold: " + BounceThreshold);
        sb.AppendLine("<br/>   Max Angular Velocity: " + MaxAngularVelocity);
        sb.AppendLine("<br/>   Min Penetration For Penalty: " + MinPenetrationForPenalty);
        sb.AppendLine("<br/>   Penetration Penalty Force: " + PenetrationPenaltyForce);
        sb.AppendLine("<br/>   Sleep Angular Velocity: " + SleepAngularVelocity);
        sb.AppendLine("<br/>   Sleep Velocity: " + SleepVelocity);
        sb.AppendLine("<br/>   Solver Iteration Count: " + SolverIterationCount);

        sb.AppendLine("<br/><h3>UNITY RENDERING INFO</h3>");
        sb.AppendLine("        Current Resolution: " + CurrentResolution);
        sb.AppendLine("<br/>   Ambient Light: " + AmbientLight);
        sb.AppendLine("<br/>   Flare Strength: " + FlareStrength);
        sb.AppendLine("<br/>   Fog Enabled: " + FogEnabled);
        sb.AppendLine("<br/>   Fog Color: " + FogColor);
        sb.AppendLine("<br/>   Fog Density: " + FogDensity);
        sb.AppendLine("<br/>   Halo Strength: " + HaloStrength);

        sb.AppendLine("<br/><h3>UNITY QUALITY SETTINGS INFO</h3>");
        sb.AppendLine("        Current Quality Level: " + CurrentQualityLevel);
        sb.AppendLine("<br/>   Anisotropic Filtering: " + AnisotropicFiltering);
        sb.AppendLine("<br/>   Master Texture Limit: " + MasterTextureLimit);
        sb.AppendLine("<br/>   Max Queued Frames: " + MaxQueuedFrames);
        sb.AppendLine("<br/>   Pixel Light Count: " + PixelLightCount);
        sb.AppendLine("<br/>   Shadow Cascades: " + ShadowCascades);
        sb.AppendLine("<br/>   Shadow Distance: " + ShadowDistance);
        sb.AppendLine("<br/>   Soft Vegetation Enabled: " + SoftVegetationEnabled);

        sb.AppendLine("<br/><h3>BROWSER INFO</h3>");
        sb.AppendLine("        Browser Identifier: " + BrowserIdentifier);
        sb.AppendLine("<br/>   Browser Version: " + BrowserVersion);
        sb.AppendLine("<br/>   Browser Major Version: " + BrowserMajorVersion);
        sb.AppendLine("<br/>   Browser Minor Version: " + BrowserMinorVersion);
        sb.AppendLine("<br/>   Browser Engine: " + BrowserEngine);
        sb.AppendLine("<br/>   Browser Engine Version: " + BrowserEngineVersion);
        sb.AppendLine("<br/>   Browser User Agent: " + BrowserUserAgent);

        sb.AppendLine("<br/><h3>GAME SERVER INFO</h3>");
        if (GameServerManager.Instance.PhotonServerCount > 0)
        {
            foreach (var gs in GameServerManager.Instance.PhotonServerList)
                sb.AppendLine(string.Format(" Server:{0} Ping:{1}<br/>", gs.ConnectionString, gs.Latency));
        }
        else
        {
            sb.AppendLine("No Game Server Information available.<br/>");
        }
        sb.AppendLine("<h3>END OF REPORT</h3>");
        return sb.ToString();
    }
}