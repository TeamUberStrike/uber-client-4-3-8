using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UberStrike.DataCenter.Common.Entities;

public class SkinnedMeshCombiner
{
    public static GameObject CreateCharacter(bool isHologram, GameObject baseAvatar, GameObject gearHead, GameObject gearFace, GameObject gearGloves, GameObject gearUpperBody, GameObject gearLowerBody, GameObject gearBoots)
    {
        if (isHologram)
        {
            gearHead = null;
            gearFace = null;
            gearGloves = null;
            gearUpperBody = null;
            gearLowerBody = null;
            gearBoots = null;
        }

        List<GameObject> tmp = new List<GameObject>();
        List<SkinnedMeshRenderer> smrList = new List<SkinnedMeshRenderer>();

        if (gearBoots) tmp.Add(GameObject.Instantiate(gearBoots) as GameObject);
        if (gearFace) tmp.Add(GameObject.Instantiate(gearFace) as GameObject);
        if (gearGloves) tmp.Add(GameObject.Instantiate(gearGloves) as GameObject);
        if (gearHead) tmp.Add(GameObject.Instantiate(gearHead) as GameObject);
        if (gearLowerBody) tmp.Add(GameObject.Instantiate(gearLowerBody) as GameObject);
        if (gearUpperBody) tmp.Add(GameObject.Instantiate(gearUpperBody) as GameObject);

        foreach (var i in tmp)
            smrList.AddRange(i.GetComponentsInChildren<SkinnedMeshRenderer>(true));

        GameObject returnGameObject = (GameObject)GameObject.Instantiate(baseAvatar);

        SuperCombineCreate(returnGameObject, smrList);

        // Disable receive shadows for the avatar
        returnGameObject.GetComponent<SkinnedMeshRenderer>().receiveShadows = false;

        for (int i = 0; i < tmp.Count; i++)
            GameObject.Destroy(tmp[i]);

        return returnGameObject;
    }

    private static GameObject SuperCombineCreate(GameObject sourceGameObject, List<SkinnedMeshRenderer> otherGear)
    {
        foreach (SkinnedMeshRenderer smr in otherGear)
        {
            if (smr.sharedMesh == null)
            {
                Debug.LogError(smr.name + "'s sharedMesh is null!");
            }
        }

        List<CombineInstance> masterCombineInstances = new List<CombineInstance>();
        List<Material> masterMaterials = new List<Material>();
        List<Transform> masterBones = new List<Transform>();
        Dictionary<string, Transform> sourceGameObjectBonesTransforms = new Dictionary<string, Transform>();

        //Iterate the transforms of our rootGameObject and grab the transforms, so we can lookup the matching bones names later (when we come to adding the bones from the skinnedMeshToCombine)
        foreach (Transform rootGoTransform in sourceGameObject.GetComponentsInChildren<Transform>(true))
        {
            sourceGameObjectBonesTransforms.Add(rootGoTransform.name, rootGoTransform.transform);
        }

        //Iterate through the skinned meshes we want to combine
        foreach (SkinnedMeshRenderer skinnedMeshToCombine in sourceGameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            //Add the materials on the skinned mesh to our master list of materials
            masterMaterials.AddRange(skinnedMeshToCombine.sharedMaterials);

            //Add the submeshes to the master combineinstances list
            for (int subMeshIndex = 0; subMeshIndex < skinnedMeshToCombine.sharedMesh.subMeshCount; subMeshIndex++)
            {
                CombineInstance combineInstance = new CombineInstance();
                combineInstance.mesh = skinnedMeshToCombine.sharedMesh;
                combineInstance.subMeshIndex = subMeshIndex;
                masterCombineInstances.Add(combineInstance);
                //Add the Transforms for the bones reference
                masterBones.AddRange(skinnedMeshToCombine.bones);
            }

            Object.Destroy(skinnedMeshToCombine);
        }

        if (otherGear != null && otherGear.Count > 0)
        {
            foreach (SkinnedMeshRenderer skinnedMeshToCombine in otherGear)
            {
                //Add the materials on the skinned mesh to our master list of materials
                masterMaterials.AddRange(skinnedMeshToCombine.sharedMaterials);

                if (skinnedMeshToCombine.sharedMesh == null)
                {
                    continue;
                }

                //Add the submeshes to the master combineinstances list
                for (int subMeshIndex = 0; subMeshIndex < skinnedMeshToCombine.sharedMesh.subMeshCount; subMeshIndex++)
                {
                    CombineInstance combineInstance = new CombineInstance();
                    combineInstance.mesh = skinnedMeshToCombine.sharedMesh;
                    combineInstance.subMeshIndex = subMeshIndex;
                    masterCombineInstances.Add(combineInstance);

                    // Add the Transforms for the bones reference
                    // Iterate through all the bone transforms on the remote gameobjects SMR and find the matching bone transform in the local gameobject
                    // Just change the remote bones transforms to reference the local bones transforms with a matching name... SWEEEET!
                    foreach (Transform t in skinnedMeshToCombine.bones)
                    {
                        if (sourceGameObjectBonesTransforms.ContainsKey(t.name))
                        {
                            //We found a transform matching the remote gameobjects bone name in the local gameobject
                            masterBones.Add(sourceGameObjectBonesTransforms[t.name]);
                        }
                        else
                        {
                            Debug.LogError("I couldn't find a matching bone transform in the gameobject you're trying to add this skinned mesh to! " + t.name);
                        }
                    }
                }

                //Object.Destroy(skinnedMeshToCombine);
            }
        }

        SkinnedMeshRenderer newSkinnedMeshRenderer = sourceGameObject.AddComponent<SkinnedMeshRenderer>();

        if (newSkinnedMeshRenderer.sharedMesh == null)
            newSkinnedMeshRenderer.sharedMesh = new Mesh();
        newSkinnedMeshRenderer.sharedMesh.Clear();
        newSkinnedMeshRenderer.sharedMesh.name = "CombinedMesh";
        newSkinnedMeshRenderer.sharedMesh.CombineMeshes(masterCombineInstances.ToArray(), false, false);
        newSkinnedMeshRenderer.bones = masterBones.ToArray();
        foreach (var m in newSkinnedMeshRenderer.materials)
        {
            GameObject.Destroy(m);
        }
        newSkinnedMeshRenderer.materials = masterMaterials.ToArray();

        Animation anim = sourceGameObject.GetComponent<Animation>();
        if (anim) anim.cullingType = AnimationCullingType.BasedOnRenderers;

        return sourceGameObject;
    }

    public static void UpdateCharacter(bool isHologram, GameObject instance, GameObject prefab, GameObject gearHead, GameObject gearFace, GameObject gearGloves, GameObject gearUpperBody, GameObject gearLowerBody, GameObject gearBoots)
    {
        if (isHologram)
        {
            gearHead = null;
            gearFace = null;
            gearGloves = null;
            gearUpperBody = null;
            gearLowerBody = null;
            gearBoots = null;
        }

        List<SkinnedMeshRenderer> smrList = new List<SkinnedMeshRenderer>();
        //get the smr for each piece of gear
        if (gearHead) smrList.AddRange(gearHead.GetComponentsInChildren<SkinnedMeshRenderer>(true));
        if (gearFace) smrList.AddRange(gearFace.GetComponentsInChildren<SkinnedMeshRenderer>(true));
        if (gearGloves) smrList.AddRange(gearGloves.GetComponentsInChildren<SkinnedMeshRenderer>(true));
        if (gearUpperBody) smrList.AddRange(gearUpperBody.GetComponentsInChildren<SkinnedMeshRenderer>(true));
        if (gearLowerBody) smrList.AddRange(gearLowerBody.GetComponentsInChildren<SkinnedMeshRenderer>(true));
        if (gearBoots) smrList.AddRange(gearBoots.GetComponentsInChildren<SkinnedMeshRenderer>(true));
        //will normally contain only the face - or in case of a holo, the complete gear set
        smrList.AddRange(prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true));

        SuperCombineUpdate(instance, smrList);

        // Disable receive shadows for the avatar
        instance.GetComponent<SkinnedMeshRenderer>().receiveShadows = false;
    }

    private static GameObject SuperCombineUpdate(GameObject sourceGameObject, List<SkinnedMeshRenderer> otherGear)
    {

        //sourceGameObject.GetComponent<Animation>().animateOnlyIfVisible = false;

        List<CombineInstance> masterCombineInstances = new List<CombineInstance>();
        List<Material> masterMaterials = new List<Material>();
        List<Transform> masterBones = new List<Transform>();
        Dictionary<string, Transform> sourceGameObjectBonesTransforms = new Dictionary<string, Transform>();

        // Iterate the transforms of our rootGameObject and grab the transforms, so we can lookup the matching bones names later (when we come to adding the bones from the skinnedMeshToCombine)
        foreach (Transform rootGoTransform in sourceGameObject.GetComponentsInChildren<Transform>(true))
        {
            if (!sourceGameObjectBonesTransforms.ContainsKey(rootGoTransform.name))
            {
                sourceGameObjectBonesTransforms.Add(rootGoTransform.name, rootGoTransform.transform);
            }
        }

        if (otherGear != null && otherGear.Count > 0)
        {
            foreach (SkinnedMeshRenderer skinnedMeshToCombine in otherGear)
            {
                // Add the materials on the skinned mesh to our master list of materials
                masterMaterials.AddRange(skinnedMeshToCombine.sharedMaterials);

                if (skinnedMeshToCombine.sharedMesh == null)
                {
                    continue;
                }

                //Add the submeshes to the master combineinstances list
                for (int subMeshIndex = 0; subMeshIndex < skinnedMeshToCombine.sharedMesh.subMeshCount; subMeshIndex++)
                {
                    CombineInstance combineInstance = new CombineInstance();
                    combineInstance.mesh = skinnedMeshToCombine.sharedMesh;
                    combineInstance.subMeshIndex = subMeshIndex;
                    masterCombineInstances.Add(combineInstance);

                    // Add the Transforms for the bones reference
                    // Iterate through all the bone transforms on the remote gameobjects SMR and find the matching bone transform in the local gameobject
                    // Just change the remote bones transforms to reference the local bones transforms with a matching name... SWEEEET!
                    foreach (Transform t in skinnedMeshToCombine.bones)
                    {
                        if (sourceGameObjectBonesTransforms.ContainsKey(t.name))
                        {
                            //We found a transform matching the remote gameobjects bone name in the local gameobject
                            masterBones.Add(sourceGameObjectBonesTransforms[t.name]);
                        }
                        else
                        {
                            Debug.LogError("I couldn't find a matching bone transform in the gameobject you're trying to add this skinned mesh to! " + t.name);
                        }
                    }
                }
            }

        }
        else
        {
            Debug.LogError("Gear array contains no Skinned Meshes! Trying to go naked?");
        }

        //Since we're doing an update, just get the existing SMR
        SkinnedMeshRenderer newSkinnedMeshRenderer = sourceGameObject.GetComponent<SkinnedMeshRenderer>();
        if (newSkinnedMeshRenderer)
        {
            if (newSkinnedMeshRenderer.sharedMesh == null)
                newSkinnedMeshRenderer.sharedMesh = new Mesh();
            newSkinnedMeshRenderer.sharedMesh.Clear();
            newSkinnedMeshRenderer.sharedMesh.name = "CombinedMesh";
            newSkinnedMeshRenderer.sharedMesh.CombineMeshes(masterCombineInstances.ToArray(), false, false);
            newSkinnedMeshRenderer.bones = masterBones.ToArray();
            foreach (var m in newSkinnedMeshRenderer.materials)
            {
                GameObject.Destroy(m);
            }
            newSkinnedMeshRenderer.materials = masterMaterials.ToArray();
        }
        else
        {
            Debug.LogError("There is no SkinnedMeshRenderer on " + sourceGameObject.name);
        }

        Animation anim = sourceGameObject.GetComponent<Animation>();
        //if (anim) anim.animateOnlyIfVisible = false;
        if (anim) anim.cullingType = AnimationCullingType.BasedOnRenderers;

        return sourceGameObject;
    }
}
