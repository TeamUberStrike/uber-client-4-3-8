using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RenderSettingsController))]
public class RenderSettingsControllerEditor : Editor
{
    private SerializedObject serObj;
    private SerializedProperty ambientLight;
    private SerializedProperty skyBox;
    private SerializedProperty fogStart;
    private SerializedProperty fogEnd;
    private SerializedProperty fogColor;
    private SerializedProperty fogUnderWaterStart;
    private SerializedProperty fogUnderWaterEnd;
    private SerializedProperty fogUnderWaterColor;
    private SerializedProperty fogDiveSpeed;
    private SerializedProperty fogSurfaceSpeed;

    private bool waterEnabled;
    private bool fogEnabled = true;

    public void OnEnable()
    {
        serObj = new SerializedObject(target);

        ambientLight = serObj.FindProperty("ambientLight");
        skyBox = serObj.FindProperty("skyBox");
        fogStart = serObj.FindProperty("fogStart");
        fogEnd = serObj.FindProperty("fogEnd");
        fogColor = serObj.FindProperty("fogColor");
        fogUnderWaterStart = serObj.FindProperty("fogUnderWaterStart");
        fogUnderWaterEnd = serObj.FindProperty("fogUnderWaterEnd");
        fogUnderWaterColor = serObj.FindProperty("fogUnderWaterColor");
        fogDiveSpeed = serObj.FindProperty("fogDiveSpeed");
        fogSurfaceSpeed = serObj.FindProperty("fogSurfaceSpeed");
    }

    public override void OnInspectorGUI()
    {
        serObj.Update();

        GameObject go = ((RenderSettingsController)serObj.targetObject).gameObject;

        if (go != null)
        {
            waterEnabled = (go.GetComponent<WaterController>() != null);

            GUILayout.Label("Configure Level Render Settings", EditorStyles.largeLabel);

            GUILayout.Label("Lighting", EditorStyles.label);
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.PropertyField(skyBox, new GUIContent("Skybox Box"));
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            ambientLight.colorValue = EditorGUILayout.ColorField("Ambient Light", ambientLight.colorValue, GUILayout.MinWidth(128));
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply", GUILayout.Width(100)))
            {
                RenderSettings.ambientLight = ambientLight.colorValue;
                RenderSettings.skybox = (Material)skyBox.objectReferenceValue;
            }
            GUILayout.Space(20);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Standard Fog", EditorStyles.label);
            GUILayout.Space(8);

            string text = (fogEnabled) ? "Preview Enabled" : "Preview Disabled";
            fogEnabled = GUILayout.Toggle(fogEnabled, text, GUILayout.Width(128));
            if (GUI.changed)
            {
                RenderSettings.fog = fogEnabled;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            fogStart.intValue = EditorGUILayout.IntField("Start", fogStart.intValue, GUILayout.MinWidth(128));
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            fogEnd.intValue = EditorGUILayout.IntField("End", fogEnd.intValue, GUILayout.MinWidth(128));
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            fogColor.colorValue = EditorGUILayout.ColorField("Color", fogColor.colorValue, GUILayout.MinWidth(128));
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply", GUILayout.Width(100)))
            {
                ApplyFog(fogStart.intValue, fogEnd.intValue, fogColor.colorValue);
            }
            GUILayout.Space(20);
            GUILayout.EndHorizontal();

            if (waterEnabled)
            {
                GUILayout.Label("Underwater Fog", EditorStyles.label);
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                fogUnderWaterStart.intValue = EditorGUILayout.IntField("Start", fogUnderWaterStart.intValue, GUILayout.MinWidth(128));
                GUILayout.Space(20);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                fogUnderWaterEnd.intValue = EditorGUILayout.IntField("End", fogUnderWaterEnd.intValue, GUILayout.MinWidth(128));
                GUILayout.Space(20);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                fogUnderWaterColor.colorValue = EditorGUILayout.ColorField("Color", fogUnderWaterColor.colorValue, GUILayout.MinWidth(128));
                GUILayout.Space(20);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                fogDiveSpeed.floatValue = EditorGUILayout.FloatField("Dive Speed", fogDiveSpeed.floatValue, GUILayout.MinWidth(128));
                GUILayout.Space(20);
                fogSurfaceSpeed.floatValue = EditorGUILayout.FloatField("Surface Speed", fogSurfaceSpeed.floatValue, GUILayout.MinWidth(128));
                GUILayout.Space(20);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Apply", GUILayout.Width(100)))
                {
                    ApplyFog(fogUnderWaterStart.intValue, fogUnderWaterEnd.intValue, fogUnderWaterColor.colorValue);
                }
                GUILayout.Space(20);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);
        }
        else
        {
            GUILayout.Label("Something Failed.", EditorStyles.miniBoldLabel);
        }

        serObj.ApplyModifiedProperties();
    }

    private void ApplyFog(float fogStart, float fogEnd, Color fogColor)
    {
        if (fogStart > fogEnd)
        {
            EditorUtility.DisplayDialog("Fog Settings Error", "Fog set to start before it ends, please check your settings.", "OK");
        }
        else
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
            RenderSettings.fogColor = fogColor;
        }
    }
}