using UnityEngine;

public class EditorOnlyMono : MonoBehaviour
{
    protected virtual void Awake()
    {
        this.enabled &= Application.isEditor;
    }
}
