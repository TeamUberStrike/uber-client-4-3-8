
using UnityEngine;

public abstract class BaseQuickItem : MonoBehaviour
{
    private void Awake()
    {
        Behaviour = new QuickItemBehaviour(this, OnActivated);
    }

    [SerializeField]
    private Texture2D _icon;

    public Texture2D Icon { get { return _icon; } set { _icon = value; } }

    public QuickItemBehaviour Behaviour { get; private set; }

    public abstract QuickItemConfiguration Configuration { get; set; }

    /// <summary>
    /// This function should be called once a QuickItem was activated and used
    /// </summary>
    protected abstract void OnActivated();

    public virtual Texture2D GetCustomIcon(QuickItemConfiguration customConfig) 
    {
        return _icon;
    }
}