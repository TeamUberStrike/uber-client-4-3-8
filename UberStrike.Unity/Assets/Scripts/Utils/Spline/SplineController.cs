using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum eOrientationMode { NODE = 0, TANGENT = 1 }

[RequireComponent(typeof(SplineInterpolator))]
public class SplineController : MonoBehaviour
{
    public GameObject Target;
    public GameObject SplineRoot;
    public float Duration = 10;
    public eOrientationMode OrientationMode = eOrientationMode.NODE;
    public eWrapMode WrapMode = eWrapMode.ONCE;
    public bool AutoStart = false;
    public bool AutoClose = false;
    public bool HideOnExecute = true;
    public bool LerpInitialPos = false;

    SplineInterpolator mSplineInterp;

    public bool SplineMovementDone
    {
        get
        {
            if (mSplineInterp != null)
            {
                return mSplineInterp.IsStopped;
            }
            else
            {
                return false;
            }
        }
    }

    private Transform[] mTransforms;

    // Shows how to follow the spline
    //private void OnDrawGizmos()
    //{
    //    Transform[] trans = GetTransforms();
    //    if (trans.Length < 2)
    //        return;

    //    SplineInterpolator interp = GetComponent(typeof(SplineInterpolator)) as SplineInterpolator;
    //    SetupSplineInterpolator(interp, trans);
    //    interp.StartInterpolation(Target, null, false, WrapMode);

    //    Vector3 prevPos = trans[0].position;
    //    for (int c = 1; c <= 1000; c++)
    //    {
    //        float currTime = c * Duration / 100;
    //        Vector3 currPos = interp.GetHermiteAtTime(currTime);

    //        float mag = (currPos - prevPos).magnitude * 2;
    //        Gizmos.color = new Color(mag, 0, 0, 1);
    //        Gizmos.DrawLine(prevPos, currPos);
    //        prevPos = currPos;
    //    }

    //    for (int i = 0; i < trans.Length; i++)
    //        Gizmos.DrawSphere(trans[i].position, 0.1f);
    //}

    void Awake()
    {
        mSplineInterp = GetComponent<SplineInterpolator>();
        UnityEngine.Profiling.Profiler.enabled = true;
    }

    IEnumerator Start()
    {
        if (HideOnExecute)
            DisableTransforms();

        while (Target == null)
            yield return new WaitForEndOfFrame();

        if (AutoStart)
            FollowSpline();
    }

    void SetupSplineInterpolator(SplineInterpolator interp, Transform[] trans)
    {
        interp.Reset();

        float step = (AutoClose) ? Duration / trans.Length :
            Duration / (trans.Length - 1);

        int c;
        for (c = 0; c < trans.Length; c++)
        {
            if (OrientationMode == eOrientationMode.NODE)
            {
                interp.AddPoint(trans[c].position, trans[c].rotation, step * c, new Vector2(0, 1));
            }
            else if (OrientationMode == eOrientationMode.TANGENT)
            {
                Quaternion rot;
                if (c != trans.Length - 1)
                    rot = Quaternion.LookRotation(trans[c + 1].position - trans[c].position, trans[c].up);
                else if (AutoClose)
                    rot = Quaternion.LookRotation(trans[0].position - trans[c].position, trans[c].up);
                else
                    rot = trans[c].rotation;

                interp.AddPoint(trans[c].position, rot, step * c, new Vector2(0, 1));
            }
        }

        if (AutoClose)
            interp.SetAutoCloseMode(step * c);
    }

    /// <summary>
    /// Returns children transforms, sorted by name.
    /// </summary>
    Transform[] GetTransforms()
    {
        if (SplineRoot != null)
        {
            List<Transform> transforms = new List<Transform>(SplineRoot.GetComponentsInChildren<Transform>(true));
            transforms.Remove(SplineRoot.transform);
            transforms.Sort((a, b) =>
            {
                return a.name.CompareTo(b.name);
            });

            return transforms.ToArray();
        }

        return null;
    }

    /// <summary>
    /// Disables the spline objects, we don't need them outside design-time.
    /// </summary>
    void DisableTransforms()
    {
        if (SplineRoot != null)
        {
            SplineRoot.SetActiveRecursively(false);
        }
    }

    /// <summary>
    /// Starts the interpolation
    /// </summary>
    public void FollowSpline(OnEndCallback callback = null)
    {
        mTransforms = GetTransforms();
        if (mTransforms.Length > 0)
        {
            UnityEngine.Profiling.Profiler.BeginSample("FollowSpline");
            if (LerpInitialPos)
            {
                /* The target will move smoothly from current position */
                mTransforms[0] = Target.transform;
            }

            SetupSplineInterpolator(mSplineInterp, mTransforms);
            mSplineInterp.StartInterpolation(Target, null, true, WrapMode);
            UnityEngine.Profiling.Profiler.EndSample();
        }
    }

    public void Reset()
    {
        if (Target && mTransforms.Length > 0)
        {
            Target.transform.position = mTransforms[0].position;
            Target.transform.rotation = mTransforms[0].rotation;
        }
    }

    public void Stop()
    {
        if (mSplineInterp)
            mSplineInterp.Stop();
    }
}