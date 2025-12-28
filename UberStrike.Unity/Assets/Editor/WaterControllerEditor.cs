using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WaterController))]
public class WaterControllerEditor : Editor
{
    private SerializedObject serObj;
    private SerializedProperty waterBase;
    private WaterController waterController;
    private SerializedProperty waterRefraction;
    private SerializedProperty waterReflection;
    private SerializedProperty underwaterRefraction;
    private SerializedProperty underwaterReflection;

    public void OnEnable()
    {
        serObj = new SerializedObject(target);
        waterController = (WaterController)serObj.targetObject;
        waterBase = serObj.FindProperty("waterBase");
        waterRefraction = serObj.FindProperty("waterRefraction");
        waterReflection = serObj.FindProperty("waterReflection");
        underwaterRefraction = serObj.FindProperty("underwaterRefraction");
        underwaterReflection = serObj.FindProperty("underwaterReflection");
    }

    public override void OnInspectorGUI()
    {
        serObj.Update();

        GameObject go = ((WaterController)serObj.targetObject).gameObject;

        if (go != null)
        {
            GUILayout.Label("Configure Water Settings", EditorStyles.largeLabel);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Water", EditorStyles.label);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.PropertyField(waterBase, new GUIContent("Water Base"));
            GUILayout.Space(20);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            waterRefraction.colorValue = EditorGUILayout.ColorField("Refraction", waterRefraction.colorValue, GUILayout.MinWidth(128));
            GUILayout.Space(20);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            waterReflection.colorValue = EditorGUILayout.ColorField("Reflection", waterReflection.colorValue, GUILayout.MinWidth(128));
            GUILayout.Space(20);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply", GUILayout.Width(100)))
            {
                waterController.waterBase = (WaterBase)waterBase.objectReferenceValue;
                waterController.waterBase.gameObject.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
                SetMaterialColor("_BaseColor", waterRefraction.colorValue, waterController.waterBase.sharedMaterial);
                SetMaterialColor("_ReflectionColor", waterReflection.colorValue, waterController.waterBase.sharedMaterial);
            }
            GUILayout.Space(20);
            GUILayout.EndHorizontal();

            // Underwater
            GUILayout.BeginHorizontal();
            GUILayout.Label("Underwater", EditorStyles.label);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            underwaterRefraction.colorValue = EditorGUILayout.ColorField("Refraction", underwaterRefraction.colorValue, GUILayout.MinWidth(128));
            GUILayout.Space(20);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            underwaterReflection.colorValue = EditorGUILayout.ColorField("Reflection", underwaterReflection.colorValue, GUILayout.MinWidth(128));
            GUILayout.Space(20);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply", GUILayout.Width(100)))
            {
                waterController.waterBase.gameObject.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 180));
                SetMaterialColor("_BaseColor", underwaterRefraction.colorValue, waterController.waterBase.sharedMaterial);
                SetMaterialColor("_ReflectionColor", underwaterReflection.colorValue, waterController.waterBase.sharedMaterial);
            }
            GUILayout.Space(20);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
        }
        else
        {
            GUILayout.Label("Something Failed.", EditorStyles.miniBoldLabel);
        }

        serObj.ApplyModifiedProperties();
    }

    private void SetMaterialColor(System.String name, Color color, Material mat)
    {
        mat.SetColor(name, color);
    }
}