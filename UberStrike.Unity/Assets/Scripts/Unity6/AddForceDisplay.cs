using UnityEngine;

/// <summary>
/// Add Force Display - Automatically adds the force display HUD for immediate visibility
/// Use this if you can't see the regular HUD overlay
/// </summary>
public class AddForceDisplay : MonoBehaviour
{
    void Start()
    {
        // Wait a moment then add force display
        Invoke("AddForceHUD", 1f);
    }
    
    void AddForceHUD()
    {
        var forceDisplay = FindObjectOfType<Unity6HUDForceDisplay>();
        if (forceDisplay == null)
        {
            GameObject forceGO = new GameObject("Unity6HUDForceDisplay");
            forceGO.AddComponent<Unity6HUDForceDisplay>();
            Debug.Log("[AddForceDisplay] 🔥 Added FORCE DISPLAY HUD - You should see bright HUD now!");
        }
    }
    
    void Update()
    {
        // Manual trigger with Enter
        if (Input.GetKeyDown(KeyCode.Return))
        {
            AddForceHUD();
        }
    }
}