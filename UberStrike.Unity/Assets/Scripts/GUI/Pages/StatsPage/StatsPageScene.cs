using UnityEngine;

public class StatsPageScene : PageScene
{
    public override PageType PageType
    {
        get { return PageType.Stats; }
    }

    protected override void OnLoad()
    {
        Vector3 position;
        Quaternion rotation;
        if (MenuConfiguration.Instance.GetPageAnchorPoint(PageType, out position, out rotation))
        {
            GameState.LocalDecorator.SetPosition(position, rotation);
        }
    }
}