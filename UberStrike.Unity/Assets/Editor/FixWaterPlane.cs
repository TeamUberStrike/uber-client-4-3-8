using UnityEngine;
using UnityEditor;

public class FixWaterPlane : MonoBehaviour
{
    [MenuItem("UberStrike/Fix WaterPlane Collider")]
    public static void Fix()
    {
        var colliders = FindObjectsOfType<MeshCollider>();
        int count = 0;
        foreach(var mc in colliders)
        {
            if(mc.gameObject.name.Contains("WaterPlane") && !mc.convex)
            {
                mc.convex = true;
                EditorUtility.SetDirty(mc);
                count++;
            }
        }
        Debug.Log($"Fixed {count} WaterPlane MeshColliders.");
    }
}
