
using UnityEngine;

public interface IPanelGui
{
    void Show();
    void Hide();
    bool IsEnabled { get; }
}

public abstract class PanelGuiBase : MonoBehaviour, IPanelGui
{
    public virtual void Show()
    {
        enabled = true;
    }

    public virtual void Hide()
    {
        enabled = false;
    }

    public bool IsEnabled { get { return enabled; } }
}