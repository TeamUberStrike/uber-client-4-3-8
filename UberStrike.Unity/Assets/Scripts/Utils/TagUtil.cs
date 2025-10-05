using UnityEngine;
using System.Collections;

public static class TagUtil
{
    public static string GetTag(Collider c)
    {
        /* Assume the default surface is cement */
        string tag = "Cement";

        try
        {
            if (c)
                tag = c.tag;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to get tag from collider: " + ex.Message);
        }

        return tag;
    }
}