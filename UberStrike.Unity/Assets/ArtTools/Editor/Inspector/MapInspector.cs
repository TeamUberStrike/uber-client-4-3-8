using UberStrike.Unity.ArtTools;
using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(MapConfiguration))]
public class MapInspector : Editor
{
    enum TestMode
    {
        None,
        SinglePlayer,
    }

    private SerializedObject serObj;
    private SerializedProperty mapId;
    private GameObject gameObject;
    private TestMode testMode = TestMode.SinglePlayer;

    private string AssetBundleNameSD
    {
        get { return "Map-" + mapId.intValue.ToString("D2") + "-" + gameObject.name + "-SD.unity3d"; }
    }

    private string AssetBundleNameHD
    {
        get { return "Map-" + mapId.intValue.ToString("D2") + "-" + gameObject.name + "-HD.unity3d"; }
    }

    private string PackageName
    {
        get { return "Map-" + mapId.intValue.ToString("D2") + "-" + gameObject.name + ".unitypackage"; }
    }

    private void OnEnable()
    {
        serObj = new SerializedObject(target);
        mapId = serObj.FindProperty("_mapId");
        gameObject = ((MapConfiguration)target).gameObject;

        EditorApplication.playmodeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playmodeStateChanged -= OnPlayModeChanged;
    }

    public override void OnInspectorGUI()
    {
        GUILayout.Label("Test Mode", EditorStyles.boldLabel);

        GUI.changed = false;
        if (GUILayout.Toggle(testMode == TestMode.None, "None") && GUI.changed)
        {
            testMode = TestMode.None;
        }
        if (GUILayout.Toggle(testMode == TestMode.SinglePlayer, "Single Player") && GUI.changed)
        {
            testMode = TestMode.SinglePlayer;
        }
        GUILayout.Space(20);

        GUILayout.Label("Configuration", EditorStyles.boldLabel);
        if (gameObject.name.StartsWith("Level")) gameObject.name = gameObject.name.Remove(0, 5);
        gameObject.name = EditorGUILayout.TextField("Name", "Level" + gameObject.name);
        if (gameObject.name.Length <= 5)
        {
            GUILayout.Label("The Map name is too short", CmuneEditorStyles.RedLabel);
        }

        mapId.intValue = EditorGUILayout.IntField("ID", mapId.intValue);
        if (mapId.intValue <= 0)
        {
            GUILayout.Label("Choose an ID greater than 0", CmuneEditorStyles.RedLabel);
        }
        GUILayout.Space(20);

        serObj.ApplyModifiedProperties();

        GUI.enabled &= !EditorApplication.isCompiling && mapId.intValue > 0 && gameObject.name.Length > 5;
        GUILayout.Label("Export", EditorStyles.boldLabel);
        if (GUILayout.Button("Export SD AssetBundle", GUILayout.Width(150)))
        {
            string path = EditorApplication.currentScene.Remove(EditorApplication.currentScene.LastIndexOf("/"));
            Debug.Log(EditorApplication.currentScene);
            if (EditorApplication.SaveCurrentSceneIfUserWantsTo())
            {
                gameObject.SetActiveRecursively(false);
                gameObject.active = true;

                //save scene
                EditorApplication.SaveScene(path + "/" + gameObject.name + ".unity");

                //export bundle
                UberstrikeMapExporter.ExportMapAssetBundle(AssetBundleNameSD, BuildTarget.WebPlayer);
            }

            return;
        }
        GUILayout.Label(AssetBundleNameSD, CmuneEditorStyles.WhiteLabel);
        GUILayout.Label(ArtAssetDefines.MapAssetBundlePath, CmuneEditorStyles.GrayLabel);
        GUILayout.Space(20);

        if (GUILayout.Button("Export HD AssetBundle", GUILayout.Width(150)))
        {
            string path = EditorApplication.currentScene.Remove(EditorApplication.currentScene.LastIndexOf("/"));
            Debug.Log(EditorApplication.currentScene);
            if (EditorApplication.SaveCurrentSceneIfUserWantsTo())
            {
                gameObject.SetActiveRecursively(false);
                gameObject.active = true;

                //save scene
                EditorApplication.SaveScene(path + "/" + gameObject.name + ".unity");

                //export bundle
                UberstrikeMapExporter.ExportMapAssetBundle(AssetBundleNameHD, BuildTarget.StandaloneWindows);
            }

            return;
        }

        GUILayout.Label(AssetBundleNameHD, CmuneEditorStyles.WhiteLabel);
        GUILayout.Label(ArtAssetDefines.MapAssetBundlePath, CmuneEditorStyles.GrayLabel);
        GUILayout.Space(20);

        if (GUILayout.Button("Export Package", GUILayout.Width(150)))
        {
            string path = EditorApplication.currentScene.Remove(EditorApplication.currentScene.LastIndexOf("/"));
            Debug.Log(EditorApplication.currentScene);
            if (EditorApplication.SaveCurrentSceneIfUserWantsTo())
            {
                //save scene
                EditorApplication.SaveScene(path + "/" + gameObject.name + ".unity");

                //export package
                UberstrikeMapExporter.ExportMapPackage(PackageName);
            }

            return;
        }
        GUILayout.Label(PackageName, CmuneEditorStyles.WhiteLabel);
        GUILayout.Label(ArtAssetDefines.MapPackagePath, CmuneEditorStyles.GrayLabel);
        GUILayout.Space(20);

        //if (GUILayout.Button("Synchronize", GUILayout.Width(150)))
        {
            //System.Diagnostics.Process.Start(@"C:\Program Files\TortoiseHg\thgw.exe");

            //if (string.IsNullOrEmpty(commitMessage))
            //{
            //    EditorUtility.DisplayDialog("Missing Commit Message", "You have to describe what you changed.", "OK");
            //}
            //else if (EditorUtility.DisplayDialog("Synchronize Assets", "Are you sure you want to upload all assets?", "YES", "Maybe not"))
            //{
            //    ProjectSynchronization.PushArtAssets(commitMessage);
            //    commitMessage = string.Empty;
            //}

            //string pathToMap = Path.Combine(ArtAssetDefines.MapAssetBundlePath, AssetBundleName);

            //string cmd1 = @"net use m: " + ArtAssetDefines.MapDeploymentPath + " cmune$1 /user:Administrator";
            //bool success = false;
            //if (ShellCommand.Create(cmd1)
            //                .SetCallback((output, error) =>
            //                {
            //                    //drive already exists
            //                    success = error.Contains("85");
            //                }).Run() || success)
            //{
            //    string cmd2 = @"echo f | xcopy " + pathToMap + " " + ArtAssetDefines.MapDeploymentPath + @"\" + AssetBundleName + " /f /y /q";
            //    ShellCommand.Create(cmd2).SetCallback((a, b) =>
            //    {
            //        UnityEditor.EditorUtility.DisplayDialog("Upload Finished", a + "\n" + b, "OK");
            //    }).RunAsync();

            //    WebserviceUtil.UpdateMapVersion(AssetBundleExporter.GetMD5HashFromFile(pathToMap), mapId.intValue, MapType.StandardDefinition);
            //}
            //else
            //{
            //    UnityEditor.EditorUtility.DisplayDialog("Error connecting to server", "", "OK");
            //}
        }
        //commitMessage = GUILayout.TextField(commitMessage, 60);
        //GUILayout.Label("Describe here what you have changed", CmuneEditorStyles.GrayLabel);
    }

    private void OnPlayModeChanged()
    {
        if (EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (testMode == TestMode.SinglePlayer)
            {
                ApplicationDataManager.AutoLogin = false;
                Application.LoadLevelAdditive("Latest");

                var map = target as MapConfiguration;
                if (map != null)
                {
                    map.gameObject.SetActiveRecursively(false);
                    GameState.SetCurrentSpace(map);

                    MonoRoutine.Start(StartLoadingGame());
                }
            }
        }
    }

    IEnumerator StartLoadingGame()
    {
        yield return new WaitForEndOfFrame();

        GameState.LocalDecorator = AvatarBuilder.Instance.CreateLocalAvatar();

        yield return new WaitForEndOfFrame();

        GameStateController.Instance.LoadGameMode(GameMode.Training);
    }
}