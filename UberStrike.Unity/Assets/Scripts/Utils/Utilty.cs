using UnityEngine;
using System.Text;

public static class Utilty
{
    public static string GetPath(Transform transform)
    {
        StringBuilder b = new StringBuilder();
        if (transform != null)
        {
            b.Append("/").Append(transform.name);
            while (transform.transform.parent != null)
            {
                transform = transform.parent;
                b.Insert(0, "/" + transform.name);
            }
        }
        else
        {
            b.Append("null");
        }
        return b.ToString();
    }
}