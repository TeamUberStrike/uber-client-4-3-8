using UnityEngine;

public class SceneInspector : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== SCENE HIERARCHY INSPECTION ===");
        InspectScene();
    }
    
    void InspectScene()
    {
        // Get all root GameObjects
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        Debug.Log($"Root GameObjects in scene: {rootObjects.Length}");
        
        foreach (GameObject root in rootObjects)
        {
            Debug.Log($"ROOT: {root.name} - Active: {root.activeInHierarchy}");
            InspectGameObject(root, 1);
        }
        
        // Specific search for key components
        Debug.Log("\n=== KEY COMPONENT SEARCH ===");
        
        MenuPageManager[] menuManagers = FindObjectsOfType<MenuPageManager>(true);
        Debug.Log($"MenuPageManager components found: {menuManagers.Length}");
        
        PageScene[] pageScenes = FindObjectsOfType<PageScene>(true);
        Debug.Log($"PageScene components found: {pageScenes.Length}");
        
        foreach (PageScene page in pageScenes)
        {
            Debug.Log($"PageScene: {page.name} - Type: {page.PageType} - Active: {page.gameObject.activeInHierarchy} - Enabled: {page.enabled}");
        }
        
        MenuConfiguration[] menuConfigs = FindObjectsOfType<MenuConfiguration>(true);
        Debug.Log($"MenuConfiguration components found: {menuConfigs.Length}");
        
        Debug.Log("=== END INSPECTION ===");
    }
    
    void InspectGameObject(GameObject obj, int depth)
    {
        if (depth > 3) return; // Limit depth to avoid spam
        
        string indent = new string(' ', depth * 2);
        
        // Check for important components
        string components = "";
        if (obj.GetComponent<MenuPageManager>()) components += "[MenuPageManager] ";
        if (obj.GetComponent<PageScene>()) components += "[PageScene] ";
        if (obj.GetComponent<MenuConfiguration>()) components += "[MenuConfiguration] ";
        if (obj.GetComponent<ApplicationDataManager>()) components += "[ApplicationDataManager] ";
        if (obj.GetComponent<Camera>()) components += "[Camera] ";
        
        if (!string.IsNullOrEmpty(components))
        {
            Debug.Log($"{indent}{obj.name} - Active: {obj.activeInHierarchy} {components}");
        }
        
        // Inspect children
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            InspectGameObject(obj.transform.GetChild(i).gameObject, depth + 1);
        }
    }
}