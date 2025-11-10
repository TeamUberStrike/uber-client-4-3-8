using UnityEngine;

public class DebugGraphics : IDebugPage
{
    public string Title
    {
        get { return "Gfx"; }
    }

    public void Draw()
    {
        GUILayout.Label("graphicsDeviceID: " + SystemInfo.graphicsDeviceID);
        GUILayout.Label("graphicsDeviceNameD: " + SystemInfo.graphicsDeviceName);
        GUILayout.Label("graphicsDeviceVendorD: " + SystemInfo.graphicsDeviceVendor);
        GUILayout.Label("graphicsDeviceVendorIDD: " + SystemInfo.graphicsDeviceVendorID);
        GUILayout.Label("graphicsDeviceVersionD: " + SystemInfo.graphicsDeviceVersion);
        GUILayout.Label("graphicsMemorySizeD: " + SystemInfo.graphicsMemorySize);
        GUILayout.Label("graphicsPixelFillrateD: " + SystemInfo.graphicsPixelFillrate);
        GUILayout.Label("graphicsShaderLevelD: " + SystemInfo.graphicsShaderLevel);
        GUILayout.Label("supportedRenderTargetCountD: " + SystemInfo.supportedRenderTargetCount);
        GUILayout.Label("supportsImageEffectsD: " + SystemInfo.supportsImageEffects);
        GUILayout.Label("supportsRenderTexturesD: " + SystemInfo.supportsRenderTextures);
        GUILayout.Label("supportsShadowsD: " + SystemInfo.supportsShadows);
        GUILayout.Label("supportsVertexPrograms: " + SystemInfo.supportsVertexPrograms);
        QualitySettings.pixelLightCount = CmuneGUI.HorizontalScrollbar("pixelLightCount: ", QualitySettings.pixelLightCount, 0, 10);
        QualitySettings.masterTextureLimit = CmuneGUI.HorizontalScrollbar("masterTextureLimit: ", QualitySettings.masterTextureLimit, 0, 20);
        QualitySettings.maxQueuedFrames = CmuneGUI.HorizontalScrollbar("maxQueuedFrames: ", QualitySettings.maxQueuedFrames, 0, 10);
        QualitySettings.maximumLODLevel = CmuneGUI.HorizontalScrollbar("maximumLODLevel: ", QualitySettings.maximumLODLevel, 0, 7);
        QualitySettings.vSyncCount = CmuneGUI.HorizontalScrollbar("vSyncCount: ", QualitySettings.vSyncCount, 0, 2);
        QualitySettings.antiAliasing = CmuneGUI.HorizontalScrollbar("antiAliasing: ", QualitySettings.antiAliasing, 0, 4);
        QualitySettings.lodBias = CmuneGUI.HorizontalScrollbar("lodBias: ", QualitySettings.lodBias, 0, 4);
        Shader.globalMaximumLOD = CmuneGUI.HorizontalScrollbar("globalMaximumLOD: ", Shader.globalMaximumLOD, 100, 600);
    }
}

public static class CmuneGUI
{
    public static int HorizontalScrollbar(string title, int value, int min, int max)
    {
        float v = value;
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label(title);
            GUILayout.Space(10);
            v = GUILayout.HorizontalScrollbar(value, 1, min, max + 1);
            GUILayout.Space(10);
            GUILayout.Label(string.Format("{0} [{1},{2}]", value, min, max));
        }
        GUILayout.EndHorizontal();
        return (int)v;
    }

    public static float HorizontalScrollbar(string title, float value, int min, int max)
    {
        float v = value;
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label(title);
            GUILayout.Space(10);
            v = GUILayout.HorizontalScrollbar(value, 1, min, max + 1);
            GUILayout.Space(10);
            GUILayout.Label(string.Format("{0} [{1},{2}]", value, min, max));
        }
        GUILayout.EndHorizontal();
        return v;
    }
}