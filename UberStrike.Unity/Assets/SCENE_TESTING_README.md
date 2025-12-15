# UberStrike Scene Testing Setup

## Problem
When you play Unity scenes directly in the editor, the player/camera doesn't spawn because the game is designed around a complex multiplayer state management system that doesn't initialize properly in standalone scene mode.

## Solution
I've created a `ScenePlayTester` script that automatically initializes the player and camera systems when you enter play mode in Unity Editor.

## Setup Instructions

### Option 1: Quick Setup (Add to any scene)
1. Open any Unity scene you want to test
2. Create an empty GameObject in the scene
3. Attach the `ScenePlayTester` script to this GameObject
4. Press Play - the player should now spawn automatically!

### Option 2: Prefab Setup (Recommended)
1. Create a prefab from the GameObject with ScenePlayTester attached
2. Place this prefab in any scene you want to test
3. The script will automatically detect the scene configuration and set up the player

## How It Works

The ScenePlayTester script:
1. **Detects Scene Type**: Looks for MapConfiguration components or uses LevelManager
2. **Initializes Game Systems**: Sets up GameState, LevelCamera, and SpawnPointManager
3. **Spawns Player**: Places the player at the first available spawn point
4. **Provides Controls**: Adds on-screen buttons for respawning and changing camera modes

## Testing Controls

When the script is active, you'll see on-screen controls:
- **Respawn**: Teleport player to first spawn point
- **1st Person**: Switch to first-person view
- **3rd Person**: Switch to third-person view  
- **Free Move**: Enable spectator/free-fly camera mode

## Configuration

You can adjust the ScenePlayTester settings in the inspector:
- `Enable Auto Test`: Toggle automatic initialization
- `Spawn At First Spawn Point`: Whether to automatically spawn player
- `Player State`: Default camera mode (FirstPerson, ThirdPerson, etc.)

## Troubleshooting

### If player still doesn't spawn:
1. Check that your scene has SpawnPoint objects
2. Verify GameState and LevelCamera singletons exist in the scene
3. Look at Console logs for ScenePlayTester debug messages

### If camera is weird:
- Try the different camera mode buttons in the on-screen GUI
- Make sure your scene has a proper Camera object
- Check that MapConfiguration has DefaultViewPoint set up

### If you see errors:
- The script includes fallback initialization that should work even with missing components
- Check the Console for detailed error messages with [ScenePlayTester] prefix

## Notes

- This script only runs in the Unity Editor (not in builds)
- It mimics the initialization pattern from the existing SinglePlayer.cs component
- Works best with proper UberStrike map scenes, but has fallbacks for basic scenes
- The player will be controllable with standard FPS controls once spawned

## Example Scene Setup

For best results, your scene should have:
1. **MapConfiguration** component (preferred) or be listed in LevelManager
2. **SpawnPoint** objects scattered around the level
3. **Camera** object (typically managed by LevelCamera system)
4. **GameState** and other singleton managers (usually auto-created)

Now you should be able to test any UberStrike scene by simply hitting Play!