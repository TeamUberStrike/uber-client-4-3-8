# Unity 6 Menu System Debug Guide

The UberStrike menu system isn't appearing because the complex initialization sequence isn't working properly in Unity 6. Here's how to debug and fix it:

## 🔧 Setup Instructions

### 1. Add Diagnostic Scripts
1. **Open Latest.unity in Unity 6**
2. **Create an empty GameObject** named "Unity6DebugTools"
3. **Add these scripts to it:**
   - `Unity6InitDiagnostic.cs` - Will report what's missing
   - `Unity6MenuInitFix.cs` - Will attempt to force initialization

### 2. Check the Console
After adding the scripts, check the Unity Console for diagnostic messages:
- ✅ Green messages = working components
- ❌ Red messages = missing/broken components  
- ⚠️ Yellow messages = potential issues

## 🔍 Common Issues and Solutions

### Issue 1: MenuPageManager has no PageScene components
**Symptoms:** Console shows "PageScene components found: 0"
**Solution:** 
1. In Latest.unity, find the MenuPageManager GameObject
2. Check if it has child objects with `PageScene` components
3. If missing, the HomePage/ShopPage/StatsPage child objects need `PageScene` components attached

### Issue 2: MenuConfiguration not initialized
**Symptoms:** NullReference errors about MenuConfiguration.Instance
**Solution:**
1. Find the MenuConfiguration GameObject in Latest.unity
2. Ensure it's active and enabled
3. Check that viewPoints and anchorPoints arrays are populated in the Inspector

### Issue 3: GameState/ApplicationDataManager not loading
**Symptoms:** Console shows these components not found or not working
**Solution:**
1. Ensure ApplicationDataManager GameObject exists and is active
2. Check that EditorConfiguration.xml is in the root UberStrike folder
3. Verify LevelManager exists for space loading

### Issue 4: Camera not rendering GUI
**Symptoms:** 3D scene loads but no GUI elements visible
**Solution:**
1. Check that GUICamera exists and is enabled
2. Verify main camera has proper clear flags and culling mask
3. Ensure Canvas components (if any) have proper render modes

## 🏃‍♂️ Quick Test Steps

1. **Run the diagnostic**: Play Latest.unity with Unity6InitDiagnostic attached
2. **Check console output**: Look for the diagnostic report
3. **Try manual fix**: Use the "Force Reinitialize Menu" context menu option on Unity6MenuInitFix
4. **Test basic functionality**: 
   - Can you see any GUI elements?
   - Does the camera move properly?
   - Are there any GameObjects visible in the scene view?

## 🔧 Manual Debugging Steps

If the automatic fixes don't work:

### Step 1: Verify Scene Objects
```
GameObject.Find("MenuPageManager") // Should exist
GameObject.Find("ApplicationDataManager") // Should exist  
GameObject.Find("MenuConfiguration") // Should exist
GameObject.Find("GUICamera") // Should exist
```

### Step 2: Check Component States
- All key GameObjects should be **active** and **enabled**
- PageScene components should be attached to child objects of MenuPageManager
- MenuConfiguration should have viewPoints array populated

### Step 3: Test Minimal Initialization
Try calling manually in a test script:
```csharp
MenuPageManager.LoadPage(PageType.Home, 0f);
```

## 💡 Expected Behavior

When working correctly:
- ✅ Console shows "Unity 6 Menu Init Fix: ✅ Complete"
- ✅ You can see GUI elements on screen
- ✅ MenuPageManager reports finding PageScene components
- ✅ Camera properly positioned in the menu scene
- ✅ No NullReference exceptions in console

## 🚨 If Still Not Working

1. **Check scene hierarchy**: Ensure Latest.unity has all required GameObjects
2. **Verify component references**: Check that Inspector fields aren't missing references
3. **Test with fresh scene**: Try loading a simpler scene first to ensure Unity 6 setup works
4. **Check Unity 6 compatibility**: Some legacy components might need additional conversion

The diagnostic scripts will provide detailed information about what's missing or broken in the initialization sequence.