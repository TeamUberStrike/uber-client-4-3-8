using System;
using Cmune.Util;
using UnityEngine;

public static class LayerUtil
{
    static LayerUtil()
    {
        ValidateUberstrikeLayers();
    }

    public static void ValidateUberstrikeLayers()
    {
        for (int i = 0; i < 32; i++)
        {
            //skip the Ignore Raycast layer
            if (i == 2) continue;

            if (!string.IsNullOrEmpty(LayerMask.LayerToName(i)))
            {
                CmuneDebug.Assert(Enum.GetName(typeof(UberstrikeLayer), i) == LayerMask.LayerToName(i), "Editor layer '" + LayerMask.LayerToName(i) + "' is not defined in the UberstrikeLayer enum!");
            }
            else if (!string.IsNullOrEmpty(Enum.GetName(typeof(UberstrikeLayer), i)))
            {
                throw new Exception("UberstrikeLayer mismatch with Editor on layer: " + Enum.GetName(typeof(UberstrikeLayer), i));
            }
        }
    }

    public static int CreateLayerMask(params UberstrikeLayer[] layers)
    {
        int ret = 0;
        foreach (int i in layers)
        {
            ret = ret | (1 << i);
        }
        return ret;
    }

    public static int AddToLayerMask(int mask, params UberstrikeLayer[] layers)
    {
        foreach (int i in layers)
        {
            mask |= (1 << i);
        }
        return mask;
    }

    public static int RemoveFromLayerMask(int mask, params UberstrikeLayer[] layers)
    {
        foreach (int i in layers)
        {
            mask &= ~(1 << i);
        }
        return mask;
    }

    public static void SetLayerRecursively(Transform transform, UberstrikeLayer layer)
    {
        Transform[] tr = transform.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in tr)
        {
            t.gameObject.layer = (int)layer;
        }
    }

    public static int GetLayer(UberstrikeLayer layer)
    {
        return (int)layer;
    }

    public static bool IsLayerInMask(int mask, int layer)
    {
        return (mask & (1 << layer)) != 0;
    }

    public static bool IsLayerInMask(int mask, UberstrikeLayer layer)
    {
        return (mask & (1 << (int)layer)) != 0;
    }

    public static int LayerMaskEverything
    {
        get { return ~0; }
    }

    public static int LayerMaskNothing
    {
        get { return 0; }
    }
}