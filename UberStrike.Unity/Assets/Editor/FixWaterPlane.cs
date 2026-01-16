using UnityEngine;
using UnityEditor;

public class FixWaterPlane : MonoBehaviour
{
    [MenuItem("UberStrike/Fix WaterPlane Collider")]
    public static void Fix()
    {
        // Find WaterPlane in scene
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        int count = 0;
        
        foreach (var root in roots)
        {
            var colliders = root.GetComponentsInChildren<MeshCollider>(true);
            foreach (var col in colliders)
            {
                if (col.gameObject.name == "WaterPlane" && col.isTrigger && !col.convex)
                {
                    col.convex = true;
                    EditorUtility.SetDirty(col);
                    count++;
                }
            }
        }
        
        if(count > 0) Debug.Log($"Fixed {count} WaterPlane colliders.");
        else Debug.Log("No broken WaterPlane colliders found.");
    }
}
