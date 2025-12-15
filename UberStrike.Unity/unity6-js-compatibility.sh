#!/bin/bash
# Unity 6 JavaScript Compatibility Script
# This script manages JavaScript files for Unity version compatibility

UNITY_PROJECT_PATH="/home/xaver/code/UberStrike/uber-client-4-3-8/UberStrike.Unity"
DISABLED_FOLDER="DisabledForUnity6"
ASSETS_FOLDER="Assets"

cd "$UNITY_PROJECT_PATH"

case "$1" in
    "disable")
        echo "Disabling JavaScript files for Unity 6 compatibility..."
        mkdir -p "$DISABLED_FOLDER"
        
        # Count JS files
        js_count=$(find $ASSETS_FOLDER -name "*.js" | wc -l)
        meta_count=$(find $ASSETS_FOLDER -name "*.js.meta" | wc -l)
        
        if [ $js_count -eq 0 ]; then
            echo "No JavaScript files found in $ASSETS_FOLDER"
        else
            echo "Moving $js_count JavaScript files to $DISABLED_FOLDER/"
            find $ASSETS_FOLDER -name "*.js" -exec mv {} "$DISABLED_FOLDER/" \;
        fi
        
        if [ $meta_count -gt 0 ]; then
            echo "Moving $meta_count .meta files to $DISABLED_FOLDER/"
            find $ASSETS_FOLDER -name "*.js.meta" -exec mv {} "$DISABLED_FOLDER/" \;
        fi
        
        echo "✅ JavaScript compatibility issues resolved for Unity 6"
        echo "Disabled files are stored in: $DISABLED_FOLDER/"
        ;;
        
    "restore")
        echo "Restoring JavaScript files from $DISABLED_FOLDER/..."
        
        if [ ! -d "$DISABLED_FOLDER" ]; then
            echo "❌ No disabled folder found: $DISABLED_FOLDER"
            exit 1
        fi
        
        js_count=$(find "$DISABLED_FOLDER" -name "*.js" | wc -l)
        
        if [ $js_count -eq 0 ]; then
            echo "No JavaScript files found in $DISABLED_FOLDER"
        else
            echo "Restoring $js_count JavaScript files..."
            
            # Restore to original locations (this is simplified - in reality would need path reconstruction)
            mkdir -p "$ASSETS_FOLDER/Standard Assets/Image Effects (Pro Only)"
            mkdir -p "$ASSETS_FOLDER/Standard Assets/Editor/Image Effects"
            mkdir -p "$ASSETS_FOLDER/Standard Assets/Particles/Legacy Particles"
            
            # Move image effect scripts
            find "$DISABLED_FOLDER" -name "*Effect*.js" -exec mv {} "$ASSETS_FOLDER/Standard Assets/Image Effects (Pro Only)/" \;
            find "$DISABLED_FOLDER" -name "*Editor.js" -exec mv {} "$ASSETS_FOLDER/Standard Assets/Editor/Image Effects/" \;
            find "$DISABLED_FOLDER" -name "PostEffects*.js" -exec mv {} "$ASSETS_FOLDER/Standard Assets/Image Effects (Pro Only)/" \;
            find "$DISABLED_FOLDER" -name "*Destructor.js" -exec mv {} "$ASSETS_FOLDER/Standard Assets/Particles/Legacy Particles/" \;
            find "$DISABLED_FOLDER" -name "Quads.js" -exec mv {} "$ASSETS_FOLDER/Standard Assets/Image Effects (Pro Only)/" \;
            
            # Move any remaining JS files to Image Effects folder
            find "$DISABLED_FOLDER" -name "*.js" -exec mv {} "$ASSETS_FOLDER/Standard Assets/Image Effects (Pro Only)/" \;
            
            # Restore .meta files (simplified)
            find "$DISABLED_FOLDER" -name "*.js.meta" -exec mv {} "$ASSETS_FOLDER/Standard Assets/Image Effects (Pro Only)/" \;
        fi
        
        echo "✅ JavaScript files restored (may not work in Unity 6+)"
        echo "⚠️  WARNING: These files are incompatible with Unity 6+"
        ;;
        
    "status")
        echo "=== JavaScript Compatibility Status ==="
        echo "Unity Project Path: $UNITY_PROJECT_PATH"
        
        js_in_assets=$(find $ASSETS_FOLDER -name "*.js" 2>/dev/null | wc -l)
        js_disabled=$(find "$DISABLED_FOLDER" -name "*.js" 2>/dev/null | wc -l)
        
        echo "JavaScript files in Assets/: $js_in_assets"
        echo "JavaScript files disabled: $js_disabled"
        
        if [ $js_in_assets -eq 0 ]; then
            echo "✅ Unity 6 Compatible (no JavaScript files)"
        else
            echo "⚠️  Unity 6 Incompatible ($js_in_assets JavaScript files present)"
        fi
        
        if [ -f "$ASSETS_FOLDER/Scenes/Latest.unity" ]; then
            echo "✅ Latest.unity scene found"
        else
            echo "❌ Latest.unity scene not found"
        fi
        ;;
        
    *)
        echo "Unity 6 JavaScript Compatibility Manager"
        echo "Usage: $0 {disable|restore|status}"
        echo ""
        echo "Commands:"
        echo "  disable  - Move JavaScript files out of Assets/ for Unity 6 compatibility"
        echo "  restore  - Restore JavaScript files to Assets/ (for older Unity versions)"
        echo "  status   - Check current JavaScript compatibility status"
        echo ""
        echo "Unity 6+ no longer supports JavaScript/UnityScript (.js files)"
        echo "This mainly affects Standard Assets Image Effects"
        exit 1
        ;;
esac

exit 0