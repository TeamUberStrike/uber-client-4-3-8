using UnityEngine;

static class GraphicSettings
{
    public static void SetQualityLevel(int level)
    {
        // In case we have some funky old settings, just default
        if (level > 2 || level < 0) level = 1;

        QualitySettings.SetQualityLevel((int)level, true);
        ApplicationDataManager.ApplicationOptions.VideoQualityLevel = level;

        // Make some custom settings depending on the level
        switch (level)
        {
            case 0:
                ApplicationDataManager.ApplicationOptions.VideoBloomAndFlares = false;
                ApplicationDataManager.ApplicationOptions.VideoVignetting = false;
                ApplicationDataManager.ApplicationOptions.VideoMotionBlur = false;
                ApplicationDataManager.ApplicationOptions.VideoWaterMode = 0;
                break;
            case 1:
                ApplicationDataManager.ApplicationOptions.VideoBloomAndFlares = true;
                ApplicationDataManager.ApplicationOptions.VideoVignetting = false;
                ApplicationDataManager.ApplicationOptions.VideoMotionBlur = true;
                ApplicationDataManager.ApplicationOptions.VideoWaterMode = 1;
                break;
            case 2:
                ApplicationDataManager.ApplicationOptions.VideoBloomAndFlares = true;
                ApplicationDataManager.ApplicationOptions.VideoMotionBlur = true;
                ApplicationDataManager.ApplicationOptions.VideoVignetting = true;
                // Currently High Water4 does not work on Mac
                ApplicationDataManager.ApplicationOptions.VideoWaterMode = (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.WebGLPlayer) ? 1 : 2;
                break;
            default:
                break;
        }
    }
}