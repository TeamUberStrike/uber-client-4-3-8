using UnityEngine;
using System.Collections;

public class PerformanceTest : MonoSingleton<PerformanceTest>
{
    private bool showDialog = false;

    public static void RunPerformanceCheck()
    {
        if (SystemInfo.graphicsShaderLevel < 30)
        {
            ApplicationDataManager.ApplicationOptions.VideoBloomAndFlares = false;
            ApplicationDataManager.ApplicationOptions.VideoVignetting = false;
            ApplicationDataManager.ApplicationOptions.VideoMotionBlur = false;
            ApplicationDataManager.ApplicationOptions.VideoWaterMode = 0;

            QualitySettings.SetQualityLevel(0);
            Shader.globalMaximumLOD = 100;
            QualitySettings.maxQueuedFrames = 0;
        }
        else if (SystemInfo.supportsImageEffects)
        {
            ApplicationDataManager.ApplicationOptions.VideoBloomAndFlares = true;
            ApplicationDataManager.ApplicationOptions.VideoVignetting = true;
            ApplicationDataManager.ApplicationOptions.VideoMotionBlur = true;
        }
    }

    void Start()
    {
        if (SystemInfo.graphicsShaderLevel < 30)
        {
            if (SystemInfo.graphicsMemorySize < 128)
            {
                showDialog = true;
            }
        }
    }

    void OnGUI()
    {

        if (showDialog)
        {
            Rect rect = new Rect((Screen.width - 430) / 2, (Screen.height - 260) / 2, 430, 260);
            GUI.BeginGroup(rect, GUIContent.none, BlueStonez.window);
            {
                GUI.depth = (int)GuiDepth.Panel;
                GUI.BeginGroup(new Rect(20, 20, rect.width - 40, rect.height - 40), GUIContent.none, BlueStonez.window_standard_grey38);
                {
                    GUI.Label(new Rect(0, -50, 380, 160), "Uh Oh!", BlueStonez.label_interparkbold_32pt);
                    GUI.Label(new Rect(0, 0, 380, 160), "It looks like your computer doesn't pack", BlueStonez.label_interparkbold_13pt);
                    GUI.Label(new Rect(0, 0, 380, 190), "enough punch to run UberStrike optimally.", BlueStonez.label_interparkbold_13pt);
                    GUI.Label(new Rect(0, 0, 380, 260), "You may experience a performance hit.", BlueStonez.label_interparkbold_13pt);

                    if (GUITools.Button(new Rect(rect.width / 2 - 80, 165, 120, 32), new GUIContent(LocalizedStrings.OkCaps), BlueStonez.button_green))
                    {
                        this.enabled = false;
                    }
                }
                GUI.EndGroup();
            }
            GUI.EndGroup();
        }
    }
}