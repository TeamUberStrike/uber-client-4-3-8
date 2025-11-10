using UnityEngine;
using System.Collections;
/*
Attach this script as a parent to some game objects. The script will then combine the meshes at startup.
This is useful as a performance optimization since it is faster to render one big mesh than many small meshes. See the docs on graphics performance optimization for more info.

Different materials will cause multiple meshes to be created, thus it is useful to share as many textures/material as you can.
*/

[AddComponentMenu("Cmune/Mesh/Combine Children Pro")]
public class CombineChildrenPro : MonoBehaviour
{
    /// Usually rendering with triangle strips is faster.
    /// However when combining objects with very low triangle counts, it can be faster to use triangles.
    /// Best is to try out which value is faster in practice.
    /// This option has a far longer preprocessing time at startup but leads to better runtime performance.
    public bool generateTriangleStrips = true;
    public bool copyTangents = true;
    public bool copyVertexColors = true;
    public bool copyUV2Coordinates = true;
    public bool copyNormals = true;

    void Start()
    {
        //float startTime = Time.realtimeSinceStartup;

        //Find all Meshfilters in child GOs
        Component[] childGOMeshFilters = GetComponentsInChildren<MeshFilter>();
        Matrix4x4 myTransform = transform.worldToLocalMatrix;
        Hashtable materialToMesh = new Hashtable();

        //Iterate through the child GOs meshfilters and instance the geometry into a single mesh
        for (int i = 0; i < childGOMeshFilters.Length; i++)
        {
            MeshFilter meshFilter = (MeshFilter)childGOMeshFilters[i];
            Renderer curRenderer = childGOMeshFilters[i].renderer;
            MeshCombineUtilityPro.MeshInstance instance = new MeshCombineUtilityPro.MeshInstance();
            instance.mesh = meshFilter.sharedMesh;

            if (curRenderer != null && curRenderer.enabled && instance.mesh != null)
            {
                instance.transform = myTransform * meshFilter.transform.localToWorldMatrix;

                Material[] materials = curRenderer.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                {
                    instance.subMeshIndex = System.Math.Min(m, instance.mesh.subMeshCount - 1);

                    ArrayList objects = (ArrayList)materialToMesh[materials[m]];
                    if (objects != null)
                    {
                        objects.Add(instance);
                    }
                    else
                    {
                        objects = new ArrayList();
                        objects.Add(instance);
                        materialToMesh.Add(materials[m], objects);
                    }
                }
                curRenderer.enabled = false;
            }
        }

        foreach (DictionaryEntry dictionaryEntry in materialToMesh)
        {
            ArrayList elements = (ArrayList)dictionaryEntry.Value;
            MeshCombineUtilityPro.MeshInstance[] instances = (MeshCombineUtilityPro.MeshInstance[])elements.ToArray(typeof(MeshCombineUtilityPro.MeshInstance));
            MeshFilter filter;

            if (materialToMesh.Count == 1)
            {
                // We have a maximum of one material, so just attach the mesh to our own game object
                // Make sure we have a mesh filter & renderer
                if (GetComponent<MeshFilter>() == null) gameObject.AddComponent<MeshFilter>();
                if (!GetComponent<MeshRenderer>()) gameObject.AddComponent<MeshRenderer>();

                filter = GetComponent<MeshFilter>();
                filter.mesh = MeshCombineUtilityPro.Combine(instances, generateTriangleStrips, copyTangents, copyVertexColors, copyUV2Coordinates, copyNormals);
                renderer.material = (Material)dictionaryEntry.Key;
                renderer.enabled = true;
            }
            else
            {
                // We have multiple materials to take care of, build one mesh / gameobject for each material and parent it to this object
                GameObject go = new GameObject("Combined Mesh");
                go.layer = gameObject.layer;
                go.transform.parent = transform;
                go.transform.localScale = Vector3.one;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localPosition = Vector3.zero;
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
                go.renderer.material = (Material)dictionaryEntry.Key;
                filter = go.GetComponent<MeshFilter>();
                filter.mesh = MeshCombineUtilityPro.Combine(instances, generateTriangleStrips, copyTangents, copyVertexColors, copyUV2Coordinates, copyNormals);
            }
        }
    }
}