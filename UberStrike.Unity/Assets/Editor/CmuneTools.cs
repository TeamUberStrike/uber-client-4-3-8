using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CmuneTools
{
    [MenuItem("Cmune Tools/Utils/Fix Lightmaps")]
    static void FixLightmaps()
    {
        Object[] all = Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);
        foreach (Object o in all)
        {
            if (o is Texture2D)
            {
                string path = AssetDatabase.GetAssetPath(o.GetInstanceID());

                Debug.LogWarning(string.Format("Import Texture {0}\n{1}", o.name, path));

                TextureImporter i = TextureImporter.GetAtPath(path) as TextureImporter;
                i.textureType = TextureImporterType.Default;
                i.lightmap = true;
                i.mipmapEnabled = false;
                i.textureCompression = TextureImporterCompression.Compressed;
                i.maxTextureSize = 1024;
                var platformSettings = i.GetPlatformTextureSettings("WebGL");
                platformSettings.maxTextureSize = 512;
                platformSettings.compressionQuality = 50;
                i.SetPlatformTextureSettings(platformSettings);
                AssetDatabase.ImportAsset(path);
            }
        }
    }

    [MenuItem("Cmune Tools/Utils/Find wrongly tagged objects")]
    static void FindWrongTags()
    {
        Object[] all = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets | SelectionMode.Assets);
        int goCount = 0;
        foreach (Object o in all)
        {
            GameObject go = o as GameObject;
            if (go)
            {
                goCount++;
                if (string.IsNullOrEmpty(go.tag))
                {
                    Debug.LogError("ERROR " + go.name);
                }
                //else if (!go.tag.Equals("Untagged"))
                //{
                //    Debug.Log(go.tag + " on object " + go.name);
                //}
                //else
                //{
                //    Debug.Log(go.name + " is tagged " + go.tag);
                //}
            }
        }
        Debug.Log("done: " + goCount + "/" + all.Length);
    }

    [MenuItem("Cmune Tools/Inspector/Set FFA SpawnPoints")]
    static void SetFFASpawnPoints()
    {
        int i = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            SpawnPoint p = go.GetComponent<SpawnPoint>();
            if (p)
            {
                p.TeamPoint = UberStrike.Realtime.Common.TeamID.NONE;
                p.gameObject.name = "FFA " + (++i);
                p.GameMode = GameMode.DeathMatch;
            }
        }
    }

    [MenuItem("Cmune Tools/Inspector/Set TE Blue SpawnPoints")]
    static void SetTEBlueSpawnPoints()
    {
        int i = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            SpawnPoint p = go.GetComponent<SpawnPoint>();
            if (p)
            {
                p.TeamPoint = UberStrike.Realtime.Common.TeamID.BLUE;
                p.gameObject.name = "Blue TE " + (++i);
                p.GameMode = GameMode.TeamElimination;
            }
        }
    }

    [MenuItem("Cmune Tools/Inspector/Set TE Red SpawnPoints")]
    static void SetTERedSpawnPoints()
    {
        int i = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            SpawnPoint p = go.GetComponent<SpawnPoint>();
            if (p)
            {
                p.TeamPoint = UberStrike.Realtime.Common.TeamID.RED;
                p.gameObject.name = "Red TE " + (++i);
                p.GameMode = GameMode.TeamElimination;
            }
        }
    }

    [MenuItem("Cmune Tools/Inspector/Set TDM Blue SpawnPoints")]
    static void SetTDMBlueSpawnPoints()
    {
        int i = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            SpawnPoint p = go.GetComponent<SpawnPoint>();
            if (p)
            {
                p.TeamPoint = UberStrike.Realtime.Common.TeamID.BLUE;
                p.gameObject.name = "Blue TDM " + (++i);
                p.GameMode = GameMode.TeamDeathMatch;
            }
        }
    }

    [MenuItem("Cmune Tools/Inspector/Set TDM Red SpawnPoints")]
    static void SetTDMRedSpawnPoints()
    {
        int i = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            SpawnPoint p = go.GetComponent<SpawnPoint>();
            if (p)
            {
                p.TeamPoint = UberStrike.Realtime.Common.TeamID.RED;
                p.gameObject.name = "Red TDM " + (++i);
                p.GameMode = GameMode.TeamDeathMatch;
            }
        }
    }

    #region Lighting

    [MenuItem("Cmune Tools/Batch Routines/Lighting: Disable Cast Shadows")]
    static void disableCastShadows()
    {
        foreach (GameObject go in Selection.gameObjects)
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                r.GetComponent<Renderer>().castShadows = false;
    }

    [MenuItem("Cmune Tools/Batch Routines/Lighting: Enable Cast Shadows")]
    static void enableCastShadows()
    {
        foreach (GameObject go in Selection.gameObjects)
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                r.GetComponent<Renderer>().castShadows = true;
    }

    [MenuItem("Cmune Tools/Batch Routines/Lighting: Disable Receive Shadows")]
    static void disableRecShadows()
    {
        foreach (GameObject go in Selection.gameObjects)
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                r.GetComponent<Renderer>().receiveShadows = false;
    }

    [MenuItem("Cmune Tools/Batch Routines/Lighting: Enable Receive Shadows")]
    static void enableRecShadows()
    {
        foreach (GameObject go in Selection.gameObjects)
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                r.GetComponent<Renderer>().receiveShadows = true;
    }

    #endregion

    #region Animation

    [MenuItem("Cmune Tools/Batch Routines/Animation: Enable All")]
    static void enableAnimations()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Animation[] tempAnim = go.GetComponentsInChildren<Animation>();
            foreach (Animation anim in tempAnim)
            {
                //DestroyImmediate(mc);
                anim.enabled = true;
            }
        }
    }

    [MenuItem("Cmune Tools/Batch Routines/Animation: Disable All")]
    static void disableAnimations()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Animation[] tempAnim = go.GetComponentsInChildren<Animation>();
            foreach (Animation anim in tempAnim)
            {
                //DestroyImmediate(mc);
                anim.enabled = false;
            }
        }
    }

    [MenuItem("Cmune Tools/Batch Routines/Animation: Remove Empty")]
    static void removeEmptyAnimations()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Animation[] tempAnim = go.GetComponentsInChildren<Animation>();
            foreach (Animation anim in tempAnim)
            {
                if (anim.GetClipCount() == 0) GameObject.DestroyImmediate(anim);
            }
        }
    }

    [MenuItem("Cmune Tools/Batch Routines/Animation: Remove Disabled")]
    static void removeDisabledAnimations()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Animation[] tempAnim = go.GetComponentsInChildren<Animation>();
            foreach (Animation anim in tempAnim)
            {
                if (!anim.enabled) GameObject.DestroyImmediate(anim);
            }
        }
    }

    #endregion

    #region Mesh

    [MenuItem("Cmune Tools/Batch Routines/Collider: Remove Mesh Colliders")]
    static void removeMeshColliders()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            MeshCollider[] tempMC = go.GetComponentsInChildren<MeshCollider>();
            foreach (MeshCollider mc in tempMC)
            {
                GameObject.DestroyImmediate(mc);
            }
        }
    }

    [MenuItem("Cmune Tools/Batch Routines/Collider: Add Mesh Colliders")]
    static void addMeshColliders()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go.GetComponent<MeshCollider>() == null)
                go.AddComponent<MeshCollider>();
        }
    }

    [MenuItem("Cmune Tools/Batch Routines/Collider: Add Box Colliders")]
    static void addBoxColliders()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go.GetComponent<BoxCollider>() == null)
                go.AddComponent<BoxCollider>();
        }
    }

    [MenuItem("Cmune Tools/Batch Routines/Renderer: Disable Mesh Renderers")]
    static void disableMeshRenderers()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            MeshRenderer[] tempMR = go.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer mr in tempMR)
            {
                mr.enabled = false;
            }
        }
    }

    [MenuItem("Cmune Tools/Batch Routines/Renderer: Enable Mesh Renderers")]
    static void enableMeshRenderers()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            MeshRenderer[] tempMR = go.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer mr in tempMR)
            {
                mr.enabled = true;
            }
        }
    }

    [MenuItem("Cmune Tools/Batch Routines/Renderer: Disable Shadows (Cast and Receive)")]
    static void DisableCastREceiveShadows()
    {
        foreach (GameObject go in Selection.GetFiltered(typeof(GameObject), SelectionMode.DeepAssets | SelectionMode.Deep | SelectionMode.Assets))
        {
            Renderer[] tempMR = go.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in tempMR)
            {
                Debug.Log(r.name);

                r.castShadows = false;
                r.receiveShadows = false;
            }
        }
    }


    #endregion

    #region Transform

    private static Vector3 _transformPosition;
    private static Quaternion _transformRotation;

    [MenuItem("Cmune Tools/Copy + Paste/Transform: Copy %k")]
    static void CopyTransform()
    {
        _transformPosition = new Vector3();
        _transformRotation = new Quaternion();
        _transformPosition = Selection.activeTransform.position;
        _transformRotation = Selection.activeTransform.rotation;
    }

    [MenuItem("Cmune Tools/Copy + Paste/Transform: Paste %l")]
    static void PasteTransform()
    {
        Selection.activeGameObject.transform.position = _transformPosition;
        Selection.activeGameObject.transform.rotation = _transformRotation;
    }

    #endregion

    #region Selection

    [MenuItem("Cmune Tools/Character/Setup Avatar Bones")]
    static void SetupAvatarBones()
    {
        AvatarDecoratorConfig config = Selection.activeGameObject.GetComponent<AvatarDecoratorConfig>();
        if (config)
        {
            List<AvatarBone> bones = new List<AvatarBone>();
            foreach (var t in config.GetComponentsInChildren<Transform>(true))
            {
                if (System.Enum.IsDefined(typeof(BoneIndex), t.name))
                {
                    bones.Add(new AvatarBone() { Bone = (BoneIndex)System.Enum.Parse(typeof(BoneIndex), t.name), Transform = t });
                }
            }
            config.SetBones(bones);
        }
    }

    [MenuItem("Cmune Tools/Batch Routines/Collider: Fix Non-Uniform BoxCollider Scale %&#f")]
    static void fixNonUniformScaleSelection()
    {
        BoxCollider bc = null;
        Vector3 scale = Vector3.zero;
        int i = 0;
        int j = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            {
                bc = t.GetComponent<BoxCollider>();
                if ((t.lossyScale.x != t.lossyScale.y || t.lossyScale.x != t.lossyScale.z || t.lossyScale.y != t.lossyScale.z) && (bc != null))
                {
                    scale = t.lossyScale;
                    scale.x = scale.x * bc.size.x;
                    scale.y = scale.y * bc.size.y;
                    scale.z = scale.z * bc.size.z;
                    if (t.parent != null && (t.parent.transform.lossyScale.x != 1 || t.parent.transform.lossyScale.y != 1 || t.parent.transform.lossyScale.z != 1))
                    {
                        scale.x = scale.x / t.parent.transform.lossyScale.x;
                        scale.y = scale.y / t.parent.transform.lossyScale.y;
                        scale.z = scale.z / t.parent.transform.lossyScale.z;
                        i++;
                    }
                    bc.size = scale;
                    t.localScale = Vector3.one;
                    Debug.LogWarning(string.Format("Fixed {0}", t.name));
                    j++;
                }
            }
        }
        Debug.LogError(string.Format("Fixed {0}, from them {1} with parent scale", j, i));
    }

    [MenuItem("Cmune Tools/Batch Routines/Collider: Check Non-Uniform Scale %&s")]
    static void findNonUniformScaleSelection()
    {
        //List<Object> selection = new List<Object>(Selection.objects);
        Collider c = null;
        foreach (GameObject go in Selection.gameObjects)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            {
                //if (t.lossyScale.x != t.lossyScale.y || t.lossyScale.x != t.lossyScale.z || t.lossyScale.y != t.lossyScale.z)//(t.localScale.x != 1 || t.localScale.y != 1 || t.localScale.z != 1)
                if (t.localScale.x != t.localScale.y || t.localScale.x != t.localScale.z || t.localScale.y != t.localScale.z)//(t.localScale.x != 1 || t.localScale.y != 1 || t.localScale.z != 1)
                {
                    c = t.GetComponent<Collider>();
                    if (c != null)
                    {
                        Debug.LogError(t.name + " with collider has been scaled! " + c.GetType());// + string.Format(" {0} {1} {2} {3}", t.lossyScale.x, t.lossyScale.y, t.lossyScale.z, ((int)t.lossyScale.x != (int)t.lossyScale.y)));
                    }
                    else
                    {
                        Debug.LogWarning(t.name + " has been scaled! ");
                    }
                }
            }
        }

        //Selection.objects = all = selection.ToArray();
    }

    [MenuItem("Cmune Tools/Batch Routines/Mesh: Remove MeshFilters")]
    static void removeMeshFilters()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            MeshFilter mf = go.GetComponent<MeshFilter>(); if (mf) GameObject.DestroyImmediate(mf);
            MeshRenderer mr = go.GetComponent<MeshRenderer>(); if (mr) GameObject.DestroyImmediate(mr);
        }
    }

    #endregion

    #region Beast!

    [MenuItem("Cmune Tools/Beast!/EnableAllLights")]
    static void EnableAllLights()
    {
        foreach (GameObject go in Selection.GetFiltered(typeof(GameObject), SelectionMode.Deep))
        {
            if (go.GetComponent<Light>())
            {
                go.GetComponent<Light>().enabled = true;
            }
        }
    }

    [MenuItem("Cmune Tools/Beast!/DisableAllLights")]
    static void DisableAllLights()
    {
        foreach (GameObject go in Selection.GetFiltered(typeof(GameObject), SelectionMode.Deep))
        {
            if (go.GetComponent<Light>())
            {
                go.GetComponent<Light>().enabled = false;
            }
        }
    }

    [MenuItem("Cmune Tools/Beast!/Increase Lightmap Index by One _]")]
    static void incLightmapIndexByOne()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r.gameObject.isStatic)
                    r.lightmapIndex++;
                else
                    r.lightmapIndex = 255;
            }
        }
    }

    [MenuItem("Cmune Tools/Beast!/Decrease Lightmap Index by One _[")]
    static void decLightmapIndexByOne()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) if (r.gameObject.isStatic) r.lightmapIndex--;
        }
    }

    [MenuItem("Cmune Tools/Beast!/Set Lightmap Index to Zero")]
    static void rstLightmapIndexToZero()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r.gameObject.isStatic) r.lightmapIndex = 255;
            }
        }
    }

    [MenuItem("Cmune Tools/Beast!/Reset Lightmap Index to -1")]
    static void rstLightmapIndexToNegativeOne()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r.gameObject.isStatic) r.lightmapIndex = -1;
            }
        }
    }

    [MenuItem("Cmune Tools/Beast!/Reset NonStatic Objects Lightmap Index to -1")]
    static void rstNonStaticLightmapIndexToNegativeOne()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.gameObject.isStatic) r.lightmapIndex = -1;
            }
        }
    }

    [MenuItem("Cmune Tools/Beast!/Enable Cast Receive Shadows")]
    static void enableCastReceiveShadows()
    {
        foreach (GameObject go in Selection.GetFiltered(typeof(GameObject), SelectionMode.Deep))
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.receiveShadows = true;
                r.castShadows = true;
            }
        }
    }

    [MenuItem("Cmune Tools/Beast!/Set Lights to Auto and No Shadows")]
    static void setLightVertexNoShadows()
    {
        int i = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            foreach (Light l in go.GetComponentsInChildren<Light>(true))
            {
                bool changed = false;
                if (l.shadows != LightShadows.None)
                {
                    l.shadows = LightShadows.None;
                    changed = true;
                }
                if (l.renderMode != LightRenderMode.Auto)
                {
                    l.renderMode = LightRenderMode.Auto;
                    changed = true;
                }
                if (changed) i++;
            }
        }
        Debug.Log(string.Format("Updated {0} Lights.", i));
    }

    //[MenuItem("Cmune Tools/Beast!/Set Light Culling Mask to exclude GloballyLit")]
    //static void setLightVertexNoShadows()
    //{
    //    int i = 0;
    //    foreach (GameObject go in Selection.gameObjects)
    //    {
    //        foreach (Light l in go.GetComponentsInChildren<Light>(true))
    //        {
    //            bool changed = false;
    //            if (l.shadows != LightShadows.None)
    //            {
    //                l.shadows = LightShadows.None;
    //                changed = true;
    //            }
    //            if (l.renderMode != LightRenderMode.Auto)
    //            {
    //                l.renderMode = LightRenderMode.Auto;
    //                changed = true;
    //            }
    //            if (changed) i++;
    //        }
    //    }
    //    Debug.Log(string.Format("Updated {0} Lights.", i));
    //}

    [MenuItem("Cmune Tools/Beast!/Select Static Objects")]
    static void selectStaticObjects()
    {
        List<GameObject> selectionSet = new List<GameObject>();

        foreach (GameObject go in Selection.gameObjects)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r.gameObject.isStatic)
                {
                    selectionSet.Add(r.gameObject);
                }
            }
        }

        Selection.objects = selectionSet.ToArray();
    }

    [MenuItem("Cmune Tools/Beast!/Check Non-Standard Shaders")]
    static void checkNonStandardShaders()
    {
        List<GameObject> selectionSet = new List<GameObject>();
        foreach (GameObject go in Selection.gameObjects)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r.gameObject.isStatic)
                {
                    Debug.LogWarning(r.gameObject.name);
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (m.shader.name != "Diffuse" && m.shader.name != "Transparent/Diffuse")
                        {
                            Debug.Log(m.shader.name);
                            selectionSet.Add(r.gameObject);
                        }
                    }
                }
            }
        }
        Selection.objects = selectionSet.ToArray();
    }

    #endregion

    //[MenuItem("Cmune Tools/Sound Dependency Finder")]
    //static void FindSound()
    //{
    //    foreach (GameObject obj in Selection.gameObjects)
    //    {
    //        AudioSource[] children = obj.GetComponentsInChildren<AudioSource>(true);
    //        if (children != null)
    //        {
    //            foreach (AudioSource s in children)
    //            {
    //                if (s.clip && s.clip.name.Contains("TallColumnWind"))
    //                {
    //                    Debug.Log(s.gameObject.name);
    //                }
    //            }
    //        }
    //    }
    //}
}
