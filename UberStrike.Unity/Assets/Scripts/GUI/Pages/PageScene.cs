using UnityEngine;

public abstract class PageScene : MonoBehaviour
{
    [SerializeField]
    private bool _haveMouseOrbitCamera = false;

    public bool HaveMouseOrbitCamera { get { return _haveMouseOrbitCamera; } }

    public abstract PageType PageType { get; }

    public bool IsEnabled { get { return this.gameObject.active; } }

    public void Load()
    {
        gameObject.active = true;

        OnLoad();
    }

    public void Unload()
    {
        gameObject.active = false;

        OnUnload();
    }

    protected virtual void OnLoad() { }
    protected virtual void OnUnload() { }
}