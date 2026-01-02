using System.Collections.Generic;
using Cmune.Util;
using UberStrike.DataCenter.Common.Entities;
using UberStrike.Realtime.Common;
using UnityEngine;
using UberStrike.Core.Types;

public class QuickItemGroupHud
{
    public Animatable2DGroup Group { get { return _quickItemsGroup; } }
    public bool Enabled
    {
        get { return _quickItemsGroup.IsVisible; }
        set
        {
            if (value)
            {
                _quickItemsGroup.Show();
                ResetQuickItemVisibility();
            }
            else
            {
                _quickItemsGroup.Hide();
            }
        }
    }

    public QuickItemGroupHud()
    {
        if (HudAssets.Exists)
        {
            _quickItemSlots = new List<QuickItemHud>(3);
            _quickItemsGroup = new Animatable2DGroup();

            _quickItemSlots.Add(new QuickItemHud("Slot A-"));
            _quickItemSlots.Add(new QuickItemHud("Slot B-"));
            _quickItemSlots.Add(new QuickItemHud("Slot C-"));
            foreach (QuickItemHud quickItemHud in _quickItemSlots)
            {
                _quickItemsGroup.Group.Add(quickItemHud.Group);
            }

            ResetQuickItemsTransform();
            ResetQuickItemVisibility();

            CmuneEventHandler.AddListener<ScreenResolutionEvent>(OnScreenResolutionChange);
            CmuneEventHandler.AddListener<InputAssignmentEvent>(OnInputAssignmentChange);
        }
    }

    public void SetSelected(int slotIndex, bool moveNext = true)
    {
        for (int i = 0; i < _quickItemSlots.Count; i++)
        {
            _quickItemSlots[i].SetSelected(slotIndex == i, moveNext);
        }
    }

    public void Draw()
    {
        _quickItemsGroup.Draw();
    }

    public void ConfigureQuickItem(int slot, QuickItemConfiguration quickItem, TeamID team = TeamID.NONE)
    {
        if (_quickItemSlots.Count > slot && slot >= 0)
        {
            QuickItemHud quickItemSlot = _quickItemSlots[slot];

            if (quickItem != null)
            {
                quickItemSlot.SetRechargeBarVisible(quickItem.RechargeTime > 0);
                quickItemSlot.SetKeyBinding(InputManager.Instance.GetKeyAssignmentString(GameInputKey.QuickItem1 + slot));

                if (team == TeamID.RED)
                {
                    quickItemSlot.ConfigureSlot(HudStyleUtility.DEFAULT_RED_COLOR,
                                                ConsumableHudTextures.CircleRed,
                                                ConsumableHudTextures.CircleWhite,
                                                ConsumableHudTextures.CircleRed,
                                                ConsumableHudTextures.CircleRed,
                                                GetIconRed(quickItem));

                }
                else
                {
                    quickItemSlot.ConfigureSlot(HudStyleUtility.DEFAULT_BLUE_COLOR,
                                                ConsumableHudTextures.CircleBlue,
                                                ConsumableHudTextures.CircleWhite,
                                                ConsumableHudTextures.CircleBlue,
                                                ConsumableHudTextures.CircleBlue,
                                                GetIconBlue(quickItem));
                }
            }
            else
            {
                quickItemSlot.ConfigureEmptySlot();
            }
        }

        //update all positions
        ResetQuickItemsTransform();
    }

    public QuickItemHud GetLoadoutQuickItemHud(int slot)
    {
        if (_quickItemSlots.Count > slot && slot >= 0)
        {
            return _quickItemSlots[slot];
        }
        return null;
    }

    public void Expand()
    {
        if (ApplicationDataManager.IsMobile) return;
        int n = 0;
        for (int i = 0; i < _quickItemSlots.Count; i++)
        {
            if (!_quickItemSlots[i].IsEmpty)
            {
                _quickItemSlots[i].Expand(new Vector2(0, _quickItemSlots[i].ExpandedHeight * (n - 3)), n * 0.1f);
                ++n;
            }
        }
    }

    public void Collapse()
    {
        if (ApplicationDataManager.IsMobile) return;
        int n = 0;
        for (int i = 0; i < _quickItemSlots.Count; i++)
        {
            if (!_quickItemSlots[i].IsEmpty)
            {
                _quickItemSlots[i].Collapse(new Vector2(0, _quickItemSlots[i].CollapsedHeight * (n - 3)), n * 0.1f);
                ++n;
            }
        }
    }

    #region Private

    private Texture2D GetIconBlue(QuickItemConfiguration config)
    {
        switch (config.BehaviourType)
        {
            case QuickItemLogic.AmmoPack:
                return ConsumableHudTextures.AmmoBlue;
            case QuickItemLogic.ArmorPack:
                return ConsumableHudTextures.ArmorBlue;
            case QuickItemLogic.HealthPack:
                return ConsumableHudTextures.HealthBlue;
            case QuickItemLogic.SpringGrenade:
                return ConsumableHudTextures.SpringGrenadeBlue;
            case QuickItemLogic.ExplosiveGrenade:
                return ConsumableHudTextures.OffensiveGrenadeBlue;
            default:
                return ConsumableHudTextures.AmmoBlue;
        }
    }

    private Texture2D GetIconRed(QuickItemConfiguration config)
    {
        switch (config.BehaviourType)
        {
            case QuickItemLogic.AmmoPack:
                return ConsumableHudTextures.AmmoRed;
            case QuickItemLogic.ArmorPack:
                return ConsumableHudTextures.ArmorRed;
            case QuickItemLogic.HealthPack:
                return ConsumableHudTextures.HealthRed;
            case QuickItemLogic.SpringGrenade:
                return ConsumableHudTextures.SpringGrenadeRed;
            case QuickItemLogic.ExplosiveGrenade:
                return ConsumableHudTextures.OffensiveGrenadeRed;
            default:
                return ConsumableHudTextures.AmmoRed;
        }
    }

    private void ResetQuickItemVisibility()
    {
        if (_quickItemSlots.Count == 0)
        {
            _quickItemsGroup.Hide();
            return;
        }

        _quickItemsGroup.Show();
        foreach (var slot in _quickItemSlots)
        {
            if (slot.IsEmpty)
            {
                slot.ConfigureEmptySlot();
            }
        }
    }

    private void OnScreenResolutionChange(ScreenResolutionEvent ev)
    {
        ResetQuickItemsTransform();
    }

    private void OnInputAssignmentChange(InputAssignmentEvent ev)
    {
        for (int i = 0; i < _quickItemSlots.Count; i++)
        {
            QuickItemHud slot = _quickItemSlots[i];
            if (!slot.IsEmpty)
            {
                slot.SetKeyBinding(InputManager.Instance.GetKeyAssignmentString(GameInputKey.QuickItem1 + i));
            }
        }
    }

    private void ResetQuickItemsTransform()
    {
        var slot = _quickItemSlots[0];

        foreach (var quickItemHud in _quickItemSlots)
        {
            quickItemHud.ResetHud();
        }

        if (slot.IsExpanded)
        {
            Expand();
        }
        else
        {
            Collapse();
        }

        float height = Screen.height * 0.9f - 10;
        if (ApplicationDataManager.IsMobile) height = 160;
        _quickItemsGroup.Position = new Vector2(Screen.width * 0.95f - slot.Group.Rect.width / 2, height);
    }

    private Animatable2DGroup _quickItemsGroup;
    private List<QuickItemHud> _quickItemSlots;

    #endregion
}