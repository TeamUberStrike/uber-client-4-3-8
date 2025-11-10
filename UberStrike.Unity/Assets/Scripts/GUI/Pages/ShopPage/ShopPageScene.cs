using UnityEngine;

public class ShopPageScene : PageScene
{
    public override PageType PageType
    {
        get { return PageType.Shop; }
    }

    protected override void OnLoad()
    {
        if (!GameState.HasCurrentGame)
        {
            Vector3 position;
            Quaternion rotation;
            if (MenuConfiguration.Instance.GetPageAnchorPoint(PageType, out position, out rotation))
            {
                GameState.LocalDecorator.SetPosition(position, rotation);
            }

            AvatarAnimationManager.Instance.ResetAnimationState(PageType);

            if (GameState.LocalDecorator != null)
                GameState.LocalDecorator.HideWeapons();

            if (GameState.HasCurrentGame && !GameState.LocalPlayer.IsGamePaused)
                GameState.LocalPlayer.Pause();

            ArmorHud.Instance.Enabled = true;
        }
    }

    protected override void OnUnload()
    {
        if (!GameState.HasCurrentGame)
        {
            TemporaryLoadoutManager.Instance.ResetGearLoadout();

            ArmorHud.Instance.Enabled = false;
        }
    }
}