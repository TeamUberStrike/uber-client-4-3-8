using UnityEngine;
using Cmune.Util;

public class ClearPlayerPrefs : EditorOnlyMono
{
    public bool ClearAll = true;

    // Use this for initialization
    void OnApplicationQuit()
    {
        if (ClearAll)
        {
            CmunePrefs.Reset();
        }
    }
}
