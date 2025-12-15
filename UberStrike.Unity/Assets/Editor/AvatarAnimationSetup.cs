using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class AvatarAnimationSetup
{
    [MenuItem("Cmune Tools/Character/Setup Lutz Animations")]
    public static void SetupLutzAnimations()
    {
        Debug.LogError("SetupLutzAnimations");

        if (Selection.activeGameObject)
        {
            Animation animation = Selection.activeGameObject.GetComponent<Animation>();

            if (animation)
            {
                string path = AssetDatabase.GetAssetPath(Selection.activeObject);

                if (!string.IsNullOrEmpty(path))
                {
                    ModelImporter mi = AssetImporter.GetAtPath(path) as ModelImporter;
                    // splitAnimations is deprecated, clipAnimations handles animation splitting automatically

                    List<ModelImporterClipAnimation> animations = new List<ModelImporterClipAnimation>();
                    animations.Add(CreateAnimation(AnimationIndex.lightGunUpDown, 1, 24, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.heavyGunUpDown, 26, 49, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.run, 51, 67, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.walk, 70, 85, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.jumpUp, 87, 98, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.jumpFall, 99, 106, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.jumpLand, 107, 112, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.squat, 112, 116, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.crouch, 117, 140, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.swimStart, 142, 152, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.swimLoop, 153, 180, WrapMode.Loop, false));
                    animations.Add(CreateAnimation(AnimationIndex.leanRight, 182, 189, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.leanLeft, 189, 196, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.shootLightGun, 197, 201, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.shootHeavyGun, 202, 207, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.gotHit, 208, 208, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.die1, 209, 243, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.die2, 245, 264, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.die3, 266, 302, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.idle, 304, 337, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.lightGunBreathe, 339, 398, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.heavyGunBreathe, 400, 459, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.snipeUpDown, 460, 485, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.HomeLargeGunIdle, 486, 565, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeLargeGunLookAround, 565, 660, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeLargeGunRelaxNeck, 661, 749, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeLargeGunCheckWeapon, 750, 858, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeLargeGunShakeWeapon, 858, 938, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.meleeSwingRightToLeft, 940, 953, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.meleSwingLeftToRight, 953, 966, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeMeleeIdle, 967, 1046, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeMeleeLookAround, 1046, 1141, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeMeleeRelaxNeck, 1141, 1230, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeMeleeCheckWeapon, 1230, 1348, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeSmallGunIdle, 1349, 1428, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeSmallGunLookAround, 1428, 1523, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeSmallGunRelaxNeck, 1523, 1612, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeSmallGunCheckWeapon, 1612, 1745, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeMediumGunIdle, 1746, 1825, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeMediumGunLookAround, 1825, 1921, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeMediumGunRelaxNeck, 1921, 1989, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeMediumGunCheckWeapon, 1989, 2105, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.ShopSmallGunTakeOut, 2106, 2122, WrapMode.ClampForever, false));
                    animations.Add(CreateAnimation(AnimationIndex.ShopSmallGunAimIdle, 2122, 2182, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.ShopSmallGunShoot, 2182, 2188, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.ShopLargeGunTakeOut, 2189, 2205, WrapMode.ClampForever, false));
                    animations.Add(CreateAnimation(AnimationIndex.ShopLargeGunAimIdle, 2205, 2265, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.ShopLargeGunShoot, 2265, 2271, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.ShopHideGun, 2272, 2304, WrapMode.ClampForever, false));
                    animations.Add(CreateAnimation(AnimationIndex.ShopMeleeTakeOut, 2305, 2315, WrapMode.ClampForever, false));
                    animations.Add(CreateAnimation(AnimationIndex.ShopMeleeAimIdle, 2315, 2375, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.ShopHideMelee, 2375, 2400, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeNoWeaponIdle, 2401, 2480, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeNoWeaponnLookAround, 2480, 2575, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.HomeNoWeaponRelaxNeck, 2575, 2664, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.ShopNewGloves, 2664, 2769, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.ShopNewUpperBody, 2769, 2865, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.ShopNewBoots, 2865, 2954, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.ShopNewLowerBody, 2954, 3033, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.ShopNewHead, 3033, 3115, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.idleWalk, 3116, 3147, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.TutorialGuideWalk, 3148, 3180, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.TutorialGuideAirlock, 3181, 3311, WrapMode.Once, false));
                    animations.Add(CreateAnimation(AnimationIndex.TutorialGuideTalk, 3312, 3421, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.TutorialGuideIdle, 3422, 3501, WrapMode.Loop, true));
                    animations.Add(CreateAnimation(AnimationIndex.TutorialGuideArmory, 3502, 3576, WrapMode.Once, false));

                    mi.clipAnimations = animations.ToArray();

                    AssetDatabase.ImportAsset(path);

                    Debug.LogError("Setup Animations for " + path + " done!");
                }
                else
                {
                    Debug.LogError("asdf");
                }
            }
            else
            {
                Debug.LogError("The selected gameobject does not have an Animation Component attached!");
            }
        }
        else
        {
            Debug.LogError("Please select a gameobject first!");
        }
    }

    //[MenuItem("Cmune Tools/Character/Setup Holo")]
    //public static void SetupHolo()
    //{
    //    SetupHoloWithHelper(HoloHelper);
    //}

    [MenuItem("Cmune Tools/Character/Setup Ragdoll")]
    public static void SetupRagdoll()
    {
        //if (Selection.activeGameObject)
        //{
        //    Transform[] trans = Selection.activeGameObject.GetComponentsInChildren<Transform>();

        //    for (int i = 0; trans != null && i < trans.Length; i++)
        //    {
        //        GameObject obj = trans[i].gameObject;

        //        if (obj.collider) GameObject.DestroyImmediate(obj.collider);
        //        if (obj.rigidbody) GameObject.DestroyImmediate(obj.rigidbody);

        //        CharacterJoint j = obj.GetComponent<CharacterJoint>();
        //        if (j) GameObject.DestroyImmediate(j);
        //    }
        //}

        GameObject julia = null;
        GameObject target = null;

        if (Selection.gameObjects != null)
        {
            for (int i = 0; i < Selection.gameObjects.Length; i++)
            {
                GameObject obj = Selection.gameObjects[i];

                if (obj.name == "JuliaBaseAvatarPhysics")
                    julia = obj;
                else
                    target = obj;
            }
        }

        if (julia && target)
        {
            List<Transform> joints = new List<Transform>(julia.GetComponentsInChildren<Transform>());
            List<Transform> targets = new List<Transform>(target.GetComponentsInChildren<Transform>());

            for (int i = 0; i < joints.Count; i++)
            {
                Transform dstTransform = targets.Find(delegate(Transform t) { return t.name == joints[i].name; });

                if (!dstTransform) continue;

                if (joints[i].GetComponent<Collider>())
                {
                    if (dstTransform.GetComponent<Collider>())
                        GameObject.DestroyImmediate(dstTransform.GetComponent<Collider>());

                    if (joints[i].GetComponent<Collider>() is BoxCollider)
                    {
                        BoxCollider src = joints[i].GetComponent<Collider>() as BoxCollider;
                        BoxCollider bc = dstTransform.gameObject.AddComponent<BoxCollider>();

                        bc.size = src.size;
                        bc.center = src.center;
                    }
                    else if (joints[i].GetComponent<Collider>() is SphereCollider)
                    {
                        SphereCollider src = joints[i].GetComponent<Collider>() as SphereCollider;
                        SphereCollider sc = dstTransform.gameObject.AddComponent<SphereCollider>();

                        sc.radius = src.radius;
                        sc.center = src.center;
                    }
                    else if (joints[i].GetComponent<Collider>() is CapsuleCollider)
                    {
                        CapsuleCollider src = joints[i].GetComponent<Collider>() as CapsuleCollider;
                        CapsuleCollider cc = dstTransform.gameObject.AddComponent<CapsuleCollider>();

                        cc.radius = src.radius;
                        cc.height = src.height;
                        cc.center = src.center;
                        cc.direction = src.direction;
                    }
                }

                if (joints[i].GetComponent<Rigidbody>())
                {
                    Rigidbody dst = null;
                    Rigidbody src = joints[i].GetComponent<Rigidbody>();

                    if (dstTransform.GetComponent<Rigidbody>())
                        dst = dstTransform.GetComponent<Rigidbody>();
                    else
                        dst = dstTransform.gameObject.AddComponent<Rigidbody>();

                    dst.mass = src.mass;
                    dst.linearDamping = src.linearDamping;
                    dst.angularDamping = src.angularDamping;
                    dst.useGravity = src.useGravity;
                    dst.isKinematic = src.isKinematic;
                    dst.interpolation = src.interpolation;
                    dst.collisionDetectionMode = src.collisionDetectionMode;
                    dst.constraints = src.constraints;
                }

                CharacterJoint j = joints[i].GetComponent<CharacterJoint>();
                if (j)
                {
                    CharacterJoint dst = dstTransform.GetComponent<CharacterJoint>();

                    if (!dst) dst = dstTransform.gameObject.AddComponent<CharacterJoint>();

                    dst.connectedBody = targets.Find(delegate(Transform t) { return t.name == j.connectedBody.name; }).GetComponent<Rigidbody>();
                    dst.anchor = j.anchor;
                    dst.axis = j.axis;
                    dst.swingAxis = j.swingAxis;
                    dst.lowTwistLimit = j.lowTwistLimit;
                    dst.highTwistLimit = j.highTwistLimit;
                    dst.swing1Limit = j.swing1Limit;
                    dst.swing2Limit = j.swing2Limit;
                    dst.breakForce = j.breakForce;
                    dst.breakTorque = j.breakTorque;
                }
            }
        }

        //SetupHoloWithHelper(RagdollHelper);
    }

    [MenuItem("Cmune Tools/Character/Add BaseGameProp To Rigidbodies")]
    public static void AddBaseGamePropToRigidBodies()
    {
        if (Selection.activeGameObject)
        {
            Rigidbody[] bodies = Selection.activeGameObject.GetComponentsInChildren<Rigidbody>();

            for (int i = 0; bodies != null && i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];

                body.linearDamping = 0.5f;
                body.angularDamping = 0.1f;
                body.interpolation = RigidbodyInterpolation.None;

                //body.drag = 0;
                //body.angularDrag = 0;
                //body.interpolation = RigidbodyInterpolation.None;

                if (body.name == "Hips")
                    body.mass = 1;
                else
                    body.mass = 0.5f;

                //if (body.name == "Hips")
                //    body.mass = 30;
                //else
                //    body.mass = 0.5f;
            }
        }
    }

    private static void HoloHelper(AvatarBone bone, Transform t)
    {
        bone.Transform = t;
    }

    private static void RagdollHelper(AvatarBone bone, Transform t)
    {
        Rigidbody rb = t.GetComponent<Rigidbody>();

        if (!rb) rb = t.gameObject.AddComponent<Rigidbody>();

        if (rb)
        {
            bone.Rigidbody = rb;
        }
    }

    private delegate void Helper(AvatarBone b, Transform t);

    //private static void SetupHoloWithHelper(Helper h)
    //{
    //    BoneIndex[] boneIndices = new BoneIndex[] {
    //        BoneIndex.Hips, BoneIndex.Head, BoneIndex.Spine,
    //        BoneIndex.LeftArm, BoneIndex.LeftForeArm,
    //        BoneIndex.LeftLeg, BoneIndex.LeftUpLeg,
    //        BoneIndex.RightArm, BoneIndex.RightForeArm,
    //        BoneIndex.RightLeg, BoneIndex.RightUpLeg
    //    };

    //    List<AvatarBone> bones = new List<AvatarBone>(boneIndices.Length);
    //    List<string> boneNames = new List<string>(boneIndices.Length);

    //    if (Selection.activeGameObject)
    //    {
    //        AvatarDecorator ad = Selection.activeGameObject.GetComponent<AvatarDecorator>();
    //        if (ad)
    //        {
    //            foreach (BoneIndex i in boneIndices)
    //            {
    //                AvatarBone bone = new AvatarBone();

    //                bone.Bone = i;

    //                bones.Add(bone);

    //                boneNames.Add(System.Enum.GetName(typeof(BoneIndex), bone.Bone));
    //            }

    //            Transform[] children = ad.gameObject.GetComponentsInChildren<Transform>();
    //            foreach (Transform t in children)
    //            {
    //                for (int i = 0; i < bones.Count; i++)
    //                {
    //                    if (t.name == boneNames[i])
    //                    {
    //                        Debug.Log("Get bone: " + t.name);
    //                        if (h != null) h(bones[i], t);
    //                        break;
    //                    }
    //                }
    //            }

    //            ad.SetBones(bones);
    //        }
    //    }
    //}

    private static ModelImporterClipAnimation CreateAnimation(AnimationIndex name, int start, int stop, WrapMode wrap, bool loop)
    {
        ModelImporterClipAnimation a = new ModelImporterClipAnimation();
        a.firstFrame = start;
        a.lastFrame = stop;
        a.loop = loop;
        a.name = name.ToString();
        a.wrapMode = wrap;
        return a;
    }
}