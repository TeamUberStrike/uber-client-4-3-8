using Cmune.Util;
using UnityEngine;

public class TryItemGUI : MonoBehaviour
{
    private LoadoutArea _currentLoadoutArea;

    private void OnGUI()
    {
        if (PopupSystem.IsAnyPopupOpen || PanelManager.IsAnyPanelOpen) return;

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
        float width = Mathf.Max((Screen.width - 584) * 0.5f, 170);
        float offset = (Screen.width - 584 - width) * 0.5f;

        if (GUITools.Button(new Rect(2 + offset, Screen.height - 60, width, 32), new GUIContent(LocalizedStrings.TryYourWeapons), BlueStonez.button_white))
        {
            GameStateController.Instance.LoadTryWeaponMode();
        }
    }

    private void DrawResetGear()
    {
        float width = Mathf.Max((Screen.width - 584) * 0.5f, 170);
        float offset = (Screen.width - 584 - width) * 0.5f;

        if (TemporaryLoadoutManager.Instance.IsGearLoadoutModified)
        {
            if (GUITools.Button(new Rect(2 + offset, Screen.height - 60, width, 32), new GUIContent("Reset Avatar"), BlueStonez.button_white))
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