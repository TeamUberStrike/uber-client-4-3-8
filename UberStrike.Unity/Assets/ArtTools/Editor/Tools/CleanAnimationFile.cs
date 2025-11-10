using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class AssetUtils
{
    [MenuItem("Cmune/Selection/Clean Current Animation")]
    static void CleanLocalEulerAnglesHintCurves()
    {
        Animation animation = Selection.activeGameObject.GetComponent<Animation>();

        if (animation != null)
        {
            var curveData = new List<AnimationClipCurveData>(AnimationUtility.GetAllCurves(animation.clip, true)).FindAll(c => c.propertyName.StartsWith("m_LocalEulerAnglesHint"));
            foreach (var c in curveData)
            {
                c.curve.keys = new Keyframe[] { };
                AnimationUtility.SetEditorCurve(animation.clip, c.path, c.type, c.propertyName, c.curve);
            }

            EditorUtility.SetDirty(animation);
        }

        AssetDatabase.SaveAssets();
    }

    static void CleanOverflowingFloatCurves()
    {
        //var curveData = new List<AnimationClipCurveData>(AnimationUtility.GetAllCurves(anim, true));
        ////Debug.Log("Count: " + curveData.Count);
        ////curveData.ForEach(d => Debug.Log("Data: " + d.propertyName + " " + d.type + " " + d.curve.length));

        //float time = 0;
        //List<AnimationClipCurveData> floatsData = new List<AnimationClipCurveData>();
        //foreach (var c in curveData)
        //{
        //    if (c.propertyName.StartsWith("m_LocalEulerAnglesHint"))
        //    {
        //        floatsData.Add(c);
        //    }
        //    else
        //    {
        //        time = Mathf.Max(time, c.curve.keys[c.curve.length - 1].time);
        //    }
        //}
        //Debug.Log("Highest time: " + time);
        //foreach (var c in floatsData)
        //{
        //    c.curve.keys = new Keyframe[] { };
        //    //for (int i = 0; i < c.curve.keys.Length; i++)
        //    //{
        //    //    c.curve.RemoveKey(0);
        //    //    //if (c.curve.keys[i].time > time)
        //    //    //{
        //    //    //    c.curve.RemoveKey(i);
        //    //    //    Debug.LogWarning("Remove float");
        //    //    //}
        //    //    //else i++;
        //    //}
        //    AnimationUtility.SetEditorCurve(anim, c.path, c.type, c.propertyName, c.curve);
        //}
    }
}