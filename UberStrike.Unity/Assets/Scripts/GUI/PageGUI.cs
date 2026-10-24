
using UnityEngine;

public abstract class PageGUI : MonoBehaviour
{
    [SerializeField]
    private string _title;

    public bool IsOnGUIEnabled { get; set; }
    public abstract void DrawGUI(Rect rect);
    public virtual string Title { get { return _title; } }
}