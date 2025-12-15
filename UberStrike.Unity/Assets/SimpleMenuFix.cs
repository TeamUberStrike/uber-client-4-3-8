using UnityEngine;
using System.Collections;

public class SimpleMenuFix : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== Simple Menu Fix Started ===");
        StartCoroutine(FixMenu());
    }

    IEnumerator FixMenu()
    {
        yield return new WaitForSeconds(1f);
        
        Debug.Log("--- Checking ApplicationDataManager ---");
        var appManager = FindObjectOfType<ApplicationDataManager>();
        if (appManager == null)
            Debug.LogError("❌ ApplicationDataManager not found!");
        else
            Debug.Log("✅ ApplicationDataManager found");

        Debug.Log("--- Checking MenuPageManager ---");
        var menuManager = FindObjectOfType<MenuPageManager>();
        if (menuManager == null)
            Debug.LogError("❌ MenuPageManager not found!");
        else
        {
            Debug.Log("✅ MenuPageManager found");
            var pages = menuManager.GetComponentsInChildren<PageScene>(true);
            Debug.Log($"Found {pages.Length} PageScene components");
        }

        Debug.Log("--- Checking Cameras ---");
        var cameras = FindObjectsOfType<Camera>();
        Debug.Log($"Found {cameras.Length} cameras");
        foreach (var cam in cameras)
            Debug.Log($"Camera: {cam.name} - Active: {cam.gameObject.activeInHierarchy}");

        Debug.Log("--- Attempting to load home page ---");
        try
        {
            var menuInstance = MenuPageManager.Instance;
            if (menuInstance != null)
            {
                menuInstance.LoadPage(PageType.Home, true);
                Debug.Log("✅ Home page load attempted");
            }
            else
            {
                Debug.LogError("❌ MenuPageManager.Instance is null");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to load home page: {e.Message}");
        }
    }
}