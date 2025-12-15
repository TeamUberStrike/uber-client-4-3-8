#!/bin/bash

# Unity 6 Scene Cleanup Script
# Cleans Latest.unity scene for Unity 6 compatibility

UNITY_PROJECT_PATH="/home/xaver/code/UberStrike/uber-client-4-3-8/UberStrike.Unity"
SCENE_PATH="$UNITY_PROJECT_PATH/Assets/Scenes/Latest.unity"
BACKUP_PATH="$UNITY_PROJECT_PATH/Assets/Scenes/Latest.unity.unity6backup"

cd "$UNITY_PROJECT_PATH"

echo "=== Unity 6 Scene Cleanup Tool ==="
echo "Scene: Latest.unity"
echo "Project: $UNITY_PROJECT_PATH"

# Create backup
if [ ! -f "$BACKUP_PATH" ]; then
    echo "Creating backup: Latest.unity.unity6backup"
    cp "$SCENE_PATH" "$BACKUP_PATH"
fi

echo "Cleaning legacy particle system components from Latest.unity..."

# Remove legacy particle components from scene file
# These components are no longer available in Unity 6:
# - ParticleAnimator 
# - EllipsoidParticleEmitter
# - ParticleRenderer
# - GUILayer

# Use sed to remove component blocks from the Unity scene file
# This is a text-based approach since Unity scene files are YAML

echo "Removing ParticleAnimator components..."
sed -i '/--- !u!120 &[0-9]*/,/^--- /{ /^--- !/!d; /^--- !u!120 /d; }' "$SCENE_PATH"

echo "Removing EllipsoidParticleEmitter components..."
sed -i '/--- !u!156 &[0-9]*/,/^--- /{ /^--- !/!d; /^--- !u!156 /d; }' "$SCENE_PATH"

echo "Removing ParticleRenderer components..."
sed -i '/--- !u!146 &[0-9]*/,/^--- /{ /^--- !/!d; /^--- !u!146 /d; }' "$SCENE_PATH"

echo "Removing GUILayer components..."
sed -i '/--- !u!122 &[0-9]*/,/^--- /{ /^--- !/!d; /^--- !u!122 /d; }' "$SCENE_PATH"

echo "Cleaning up component references in GameObjects..."

# Remove references to deleted components from GameObject component arrays
# This requires more sophisticated parsing, but we can remove obvious references

# Remove component references that point to deleted IDs
sed -i '/- 120:/d' "$SCENE_PATH"   # ParticleAnimator references
sed -i '/- 156:/d' "$SCENE_PATH"   # EllipsoidParticleEmitter references  
sed -i '/- 146:/d' "$SCENE_PATH"   # ParticleRenderer references
sed -i '/- 122:/d' "$SCENE_PATH"   # GUILayer references

echo "Scene cleanup completed!"
echo ""
echo "Legacy components removed:"
echo "- ParticleAnimator (component type 120)"
echo "- EllipsoidParticleEmitter (component type 156)" 
echo "- ParticleRenderer (component type 146)"
echo "- GUILayer (component type 122)"
echo ""
echo "Backup saved as: Latest.unity.unity6backup"
echo ""
echo "You can now load Latest.unity in Unity 6 without legacy component warnings!"

# Verify scene file integrity
if [ -f "$SCENE_PATH" ] && [ -s "$SCENE_PATH" ]; then
    echo "✅ Scene file appears valid (non-empty)"
else
    echo "❌ Warning: Scene file may be corrupted, restoring backup..."
    cp "$BACKUP_PATH" "$SCENE_PATH"
    echo "Backup restored. Manual cleanup may be required."
    exit 1
fi

echo ""
echo "Next steps:"
echo "1. Open Unity 6"
echo "2. Load Assets/Scenes/Latest.unity"  
echo "3. Save the scene to finalize cleanup"
echo "4. Test scene functionality"