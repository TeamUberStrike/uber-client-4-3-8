using Cmune.Util;
using UnityEngine;

public class TryItemGUI : MonoBehaviour
{
    private LoadoutArea _currentLoadoutArea;

    // Width of the shop + loadout panel on the right that the button must stay clear of
    // (virtual units, see ShopPageGUI).
    private const float ShopPanelWidth = 584f;
    private const float MinButtonWidth = 170f;
    private const float ButtonHeight = 32f;
    private const float BottomOffset = 60f;

    private void OnGUI()
    {
        if (PopupSystem.IsAnyPopupOpen || PanelManager.IsAnyPanelOpen) return;

        // Same mobile menu scale as the shop page, so the button is laid out in the same virtual space
        // as the panel it has to avoid. Identity on PC.
        Matrix4x4 scaleMatrix = MobileMenuScale.Begin();

        switch (_currentLoadoutArea)
        {
            case LoadoutArea.Gear:
                DrawResetGear();
                break;

            case LoadoutArea.Weapons:
            case LoadoutArea.QuickItems:
                DrawTryWeapon();
                break;
        }

        MobileMenuScale.End(scaleMatrix);
    }

    /// <summary>
    /// Button rect centred on the free area left of the shop panel - the same area the camera viewport
    /// covers (which also includes the left safe-area strip, hence the BleedLeft term), so the button
    /// sits under the avatar (as on PC). Width is limited so the button stays inside the menu area and
    /// clear of the panel. On PC this matches the original layout.
    /// </summary>
    private static Rect GetButtonRect()
    {
        float available = MobileMenuScale.VirtualWidth - ShopPanelWidth;
        float center = 2 + (available - MobileMenuScale.BleedLeft) * 0.5f;
        float maxHalfWidth = Mathf.Min(center, available - center) - 2f;
        float width = Mathf.Min(Mathf.Max(available * 0.5f, MinButtonWidth), maxHalfWidth * 2f);

        return new Rect(center - width * 0.5f, MobileMenuScale.VirtualHeight - BottomOffset, width, ButtonHeight);
    }

    private void OnEnable()
    {
        CmuneEventHandler.AddListener<LoadoutAreaChangedEvent>(OnLoadoutAreaChanged);
    }

    private void OnDisable()
    {
        CmuneEventHandler.RemoveListener<LoadoutAreaChangedEvent>(OnLoadoutAreaChanged);
    }

    private void DrawTryWeapon()
    {
        if (GUITools.Button(GetButtonRect(), new GUIContent(LocalizedStrings.TryYourWeapons), BlueStonez.button_white))
        {
            GameStateController.Instance.LoadTryWeaponMode();
        }
    }

    private void DrawResetGear()
    {
        if (TemporaryLoadoutManager.Instance.IsGearLoadoutModified)
        {
            if (GUITools.Button(GetButtonRect(), new GUIContent("Reset Avatar"), BlueStonez.button_white))
            {
                TemporaryLoadoutManager.Instance.ResetGearLoadout();
            }
        }
    }

    public void OnLoadoutAreaChanged(LoadoutAreaChangedEvent ev)
    {
        _currentLoadoutArea = ev.Area;
    }
}