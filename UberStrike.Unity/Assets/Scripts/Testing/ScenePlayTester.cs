using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UberStrike.Realtime.Common;

/// <summary>
/// Automatically initializes player and camera when entering play mode in Unity Editor
/// Place this script in any scene to enable instant testing without network setup
/// </summary>
public class ScenePlayTester : MonoBehaviour
{
    [Header("Auto-Test Settings")]
    [SerializeField] private bool enableAutoTest = true;
    [SerializeField] private bool spawnAtFirstSpawnPoint = true;
    [SerializeField] private LocalPlayer.PlayerState playerState = LocalPlayer.PlayerState.FirstPerson;
    
    private void Start()
    {
        if (enableAutoTest && Application.isEditor)
        {
            StartCoroutine(InitializeTestMode());
        }
    }

    private IEnumerator InitializeTestMode()
    {
        yield return new WaitForEndOfFrame();
        
        Debug.Log("[ScenePlayTester] Starting scene test initialization...");
        
        try
        {
            // First, try to use existing map configuration from scene
            MapConfiguration mapConfig = FindObjectOfType<MapConfiguration>();
            
            if (mapConfig != null)
            {
                Debug.Log($"[ScenePlayTester] Found MapConfiguration: {mapConfig.name}");
                InitializeWithMapConfig(mapConfig);
            }
            else
            {
                Debug.Log("[ScenePlayTester] No MapConfiguration found, trying LevelManager...");
                // Try to get first available map from LevelManager (like SinglePlayer.cs does)
                if (LevelManager.Instance != null && LevelManager.Instance.AllMaps != null)
                {
                    var maps = LevelManager.Instance.AllMaps;
                    UberstrikeMap firstMap = null;
                    
                    foreach (var map in maps)
                    {
                        if (map != null && map.Id > 0)
                        {
                            firstMap = map;
                            break;
                        }
                    }
                    
                    if (firstMap != null)
                    {
                        Debug.Log($"[ScenePlayTester] Using LevelManager map: {firstMap.Name}");
                        InitializeWithMapConfig(firstMap.Space);
                    }
                    else
                    {
                        Debug.LogWarning("[ScenePlayTester] No valid maps found in LevelManager");
                        BasicFallbackInitialization();
                    }
                }
                else
                {
                    Debug.LogWarning("[ScenePlayTester] LevelManager not available");
                    BasicFallbackInitialization();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ScenePlayTester] Error during initialization: {e.Message}");
            BasicFallbackInitialization();
        }
    }
    
    private void InitializeWithMapConfig(MapConfiguration mapConfig)
    {
        StartCoroutine(InitializeWithMapConfigCoroutine(mapConfig));
    }
    
    private IEnumerator InitializeWithMapConfigCoroutine(MapConfiguration mapConfig)
    {
        // This follows the same pattern as SinglePlayer.cs
        GameState.SetCurrentSpace(mapConfig);
        
        // Setup camera
        if (GameState.CurrentSpace.Camera != null && GameState.CurrentSpace.DefaultViewPoint != null)
        {
            LevelCamera.Instance.SetLevelCamera(
                GameState.CurrentSpace.Camera,
                GameState.CurrentSpace.DefaultViewPoint.position,
                GameState.CurrentSpace.DefaultViewPoint.rotation
            );
        }
        
        // Create local avatar and character (essential for visible player)
        if (GameState.LocalDecorator == null)
        {
            Debug.Log("[ScenePlayTester] Creating local avatar...");
            GameState.LocalDecorator = AvatarBuilder.Instance.CreateLocalAvatar();
        }
        
        // Setup minimal character info for scene testing
        if (GameState.LocalCharacter.ActorId == 0)
        {
            GameState.LocalCharacter.ActorId = 1; // Dummy ID for scene testing
            GameState.LocalCharacter.PlayerName = "Scene Tester";
            GameState.LocalCharacter.TeamID = TeamID.NONE;
            GameState.LocalCharacter.Level = 1;
        }
        
        if (GameState.LocalPlayer.Character == null)
        {
            Debug.Log("[ScenePlayTester] Creating local character...");
            CharacterConfig character = PrefabManager.Instance.InstantiateLocalCharacter();
            GameState.LocalPlayer.SetCurrentCharacterConfig(character);
            
            // Initialize character with basic state (simulating network data)
            var localState = new LocalCharacterState(GameState.LocalCharacter, null);
            character.Initialize(localState, GameState.LocalDecorator);
        }
        
        // Initialize local player
        GameState.LocalPlayer.InitializePlayer();
        GameState.LocalPlayer.SetPlayerControlState(playerState);
        GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.Playing);
        
        // Configure spawn points
        if (GameState.CurrentSpace.SpawnPoints != null)
        {
            SpawnPointManager.Instance.ConfigureSpawnPoints(
                GameState.CurrentSpace.SpawnPoints.GetComponentsInChildren<SpawnPoint>(true)
            );
        }
        
        // Spawn player at first spawn point if requested
        if (spawnAtFirstSpawnPoint)
        {
            SpawnPlayerAtFirstSpawnPoint();
        }
        
        // Un-pause the player (like SinglePlayer.cs does, but immediately unpause for testing)
        yield return new WaitForSeconds(0.1f);
        GameState.LocalPlayer.UnPausePlayer();
        
        Debug.Log("[ScenePlayTester] Map-based initialization complete!");
    }
    
    private void BasicFallbackInitialization()
    {
        Debug.Log("[ScenePlayTester] Using basic fallback initialization...");
        
        try
        {
            // Try to initialize player even without a proper map
            if (GameState.LocalPlayer != null)
            {
                // Create local avatar and character (essential for visible player)
                if (GameState.LocalDecorator == null)
                {
                    Debug.Log("[ScenePlayTester] Creating local avatar (fallback)...");
                    GameState.LocalDecorator = AvatarBuilder.Instance.CreateLocalAvatar();
                }
                
                // Setup minimal character info for scene testing
                if (GameState.LocalCharacter.ActorId == 0)
                {
                    GameState.LocalCharacter.ActorId = 1; // Dummy ID for scene testing
                    GameState.LocalCharacter.PlayerName = "Scene Tester";
                    GameState.LocalCharacter.TeamID = TeamID.NONE;
                    GameState.LocalCharacter.Level = 1;
                }
                
                if (GameState.LocalPlayer.Character == null)
                {
                    Debug.Log("[ScenePlayTester] Creating local character (fallback)...");
                    CharacterConfig character = PrefabManager.Instance.InstantiateLocalCharacter();
                    GameState.LocalPlayer.SetCurrentCharacterConfig(character);
                    
                    // Initialize character with basic state (simulating network data)
                    var localState = new LocalCharacterState(GameState.LocalCharacter, null);
                    character.Initialize(localState, GameState.LocalDecorator);
                }
                
                GameState.LocalPlayer.InitializePlayer();
                GameState.LocalPlayer.SetPlayerControlState(playerState);
                GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.Playing);
                
                // Try to find camera in scene
                Camera mainCam = Camera.main;
                if (mainCam == null) mainCam = FindObjectOfType<Camera>();
                
                if (mainCam != null && LevelCamera.Instance != null)
                {
                    LevelCamera.Instance.SetLevelCamera(mainCam, mainCam.transform.position, mainCam.transform.rotation);
                }
                
                // Look for any spawn points in the scene
                SpawnPoint[] spawnPoints = FindObjectsOfType<SpawnPoint>();
                if (spawnPoints != null && spawnPoints.Length > 0)
                {
                    SpawnPointManager.Instance.ConfigureSpawnPoints(spawnPoints);
                    SpawnPlayerAtFirstSpawnPoint();
                }
                
                GameState.LocalPlayer.UnPausePlayer();
                Debug.Log("[ScenePlayTester] Basic fallback initialization complete!");
            }
            else
            {
                Debug.LogError("[ScenePlayTester] GameState.LocalPlayer not available!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ScenePlayTester] Fallback initialization failed: {e.Message}");
        }
    }
    
    private void SpawnPlayerAtFirstSpawnPoint()
    {
        try
        {
            Vector3 spawnPos;
            Quaternion spawnRot;
            
            SpawnPointManager.Instance.GetSpawnPointAt(0, GameMode.DeathMatch, TeamID.NONE, out spawnPos, out spawnRot);
            
            if (spawnPos != Vector3.zero)
            {
                GameState.LocalPlayer.SpawnPlayerAt(spawnPos, spawnRot);
                Debug.Log($"[ScenePlayTester] Player spawned at: {spawnPos}");
            }
            else
            {
                Debug.LogWarning("[ScenePlayTester] No spawn points available, using default position");
                // Try spawning at a reasonable default position
                GameState.LocalPlayer.SpawnPlayerAt(new Vector3(0, 2, 0), Quaternion.identity);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ScenePlayTester] Error spawning player: {e.Message}");
        }
    }
    
    private void OnGUI()
    {
        if (enableAutoTest && Application.isEditor && GameState.LocalPlayer != null)
        {
            GUILayout.BeginArea(new Rect(10, 10, 350, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("[SCENE PLAY TESTER ACTIVE]", GUI.skin.box);
            GUILayout.Label($"Player State: {GameState.LocalPlayer.CurrentCameraControl}");
            GUILayout.Label($"Avatar: {(GameState.LocalDecorator != null ? "✓" : "✗")}");
            GUILayout.Label($"Character: {(GameState.LocalPlayer.Character != null ? "✓" : "✗")}");
            
            if (GameState.HasCurrentSpace)
            {
                GUILayout.Label($"Map: {GameState.CurrentSpace.name}");
            }
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Respawn"))
            {
                SpawnPlayerAtFirstSpawnPoint();
            }
            
            if (GUILayout.Button("1st Person"))
            {
                GameState.LocalPlayer.SetPlayerControlState(LocalPlayer.PlayerState.FirstPerson);
            }
            
            if (GUILayout.Button("3rd Person"))
            {
                GameState.LocalPlayer.SetPlayerControlState(LocalPlayer.PlayerState.ThirdPerson);
            }
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("Free Move"))
            {
                GameState.LocalPlayer.SetPlayerControlState(LocalPlayer.PlayerState.FreeMove);
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}