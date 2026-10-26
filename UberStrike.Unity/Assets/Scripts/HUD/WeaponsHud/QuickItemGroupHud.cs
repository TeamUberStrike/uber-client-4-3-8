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
#if UNITY_EDITOR || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID
            if (TouchInput.IsSupported)
            {
                // The touch build draws the items via DrawSimple(); keep the meshes hidden.
                _quickItemsGroup.Hide();
                return;
            }
#endif
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
#if UNITY_EDITOR || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID
        if (TouchInput.IsSupported)
        {
            DrawSimple();
            return;
        }
#endif
        _quickItemsGroup.Draw();
    }

#if UNITY_EDITOR || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID
    // Draws the three items as plain icons at their saved placements - identical to the controls editor.
    public void DrawSimple()
    {
        for (int i = 0; i < _quickItemSlots.Count; i++)
        {
            QuickItemHud q = _quickItemSlots[i];
            if (q == null) continue;
            Texture icon = q.GetIcon();
            if (icon == null) continue;
            MobileControlLayout.Placement qp = MobileControlLayout.GetOrDefault("quickSlot" + (i + 1), DefaultSlotCenter(i), 1f);
            Vector2 center = MobileControlLayout.ToPixels(qp);
            float baseSize = Mathf.Max(icon.width, icon.height);
            float side = baseSize * 0.8f * qp.Scale;
            center = ClampSlotCenter(center, side * 0.5f);
            GUI.DrawTexture(new Rect(center.x - side / 2, center.y - side / 2, side, side), icon, ScaleMode.StretchToFill, true);
        }
    }

    public Rect GetSimpleCombinedRect()
    {
        Rect r = new Rect(0, 0, 0, 0);
        bool first = true;
        for (int i = 0; i < _quickItemSlots.Count; i++)
        {
            QuickItemHud q = _quickItemSlots[i];
            if (q == null) continue;
            MobileControlLayout.Placement qp = MobileControlLayout.GetOrDefault("quickSlot" + (i + 1), DefaultSlotCenter(i), 1f);
            Vector2 center = MobileControlLayout.ToPixels(qp);
            float side = 130f * qp.Scale;
            center = ClampSlotCenter(center, side * 0.5f);
            Rect sr = new Rect(center.x - side / 2, center.y - side / 2, side, side);
            if (first) { r = sr; first = false; }
            else
            {
                r.xMin = Mathf.Min(r.xMin, sr.xMin);
                r.yMin = Mathf.Min(r.yMin, sr.yMin);
                r.xMax = Mathf.Max(r.xMax, sr.xMax);
                r.yMax = Mathf.Max(r.yMax, sr.yMax);
            }
        }
        return r;
    }

    // Keeps a slot center such that the whole slot (half extents given) stays inside the display
    // at any scale — with the 3.75x maximum the saved placement alone could push slots off-screen.
    private static Vector2 ClampSlotCenter(Vector2 center, float half)
    {
        center.x = Mathf.Clamp(center.x, half, Mathf.Max(half, Screen.width - half));
        center.y = Mathf.Clamp(center.y, half, Mathf.Max(half, Screen.height - half));
        return center;
    }
#endif

    public void ConfigureQuickItem(int slot, QuickItemConfiguration quickItem, TeamID team = TeamID.NONE)
    {
        if (_quickItemSlots.Count > slot && slot >= 0)
        {
            QuickItemHud quickItemSlot = _quickItemSlots[slot];

            if (quickItem != null)
            {
                quickItemSlot.SetRechargeBarVisible(quickItem.RechargeTime > 0);
                if (!TouchInput.IsSupported)
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
#if UNITY_EDITOR || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID
        if (TouchInput.IsSupported) return;
#endif
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
#if UNITY_EDITOR || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID
        if (TouchInput.IsSupported) return;
#endif
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

    public void RefreshPosition()
    {
        if (_quickItemsGroup == null) return;
        ResetQuickItemsTransform();
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
#if UNITY_EDITOR || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID
        if (TouchInput.IsSupported)
        {
            // The touch build draws the items via DrawSimple(); the meshes stay hidden.
            _quickItemsGroup.Hide();
            return;
        }
#endif

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
        if (ApplicationDataManager.IsMobile) height = 160f;
        _quickItemsGroup.Position = new Vector2(Screen.width * 0.95f - slot.Group.Rect.width / 2, height);
    }

    public static Vector2 DefaultSlotCenter(int index)
    {
        // Vertical stack anchored to the right upper corner, directly below the weapon changer.
        float s = Mathf.Clamp(Screen.height / 720f, 1f, 2.2f);
        float x = Screen.width - 60f * s;
        float y = (105f + 150f * s) + index * 40f * s;

        // Keep the whole slot icon (~64px+ at scale 1) inside the display on any resolution.
        float half = 64f;
        x = Mathf.Clamp(x, half, Mathf.Max(half, Screen.width - half));
        y = Mathf.Clamp(y, half, Mathf.Max(half, Screen.height - half));
        return new Vector2(x, y);
    }

    public Rect GetCombinedRect()
    {
        Rect r = new Rect(0, 0, 0, 0);
        bool first = true;
        foreach (QuickItemHud q in _quickItemSlots)
        {
            if (q.IsEmpty) continue;
            Rect sr = q.Group.Rect;
            if (first) { r = sr; first = false; }
            else
            {
                r.xMin = Mathf.Min(r.xMin, sr.xMin);
                r.yMin = Mathf.Min(r.yMin, sr.yMin);
                r.xMax = Mathf.Max(r.xMax, sr.xMax);
                r.yMax = Mathf.Max(r.yMax, sr.yMax);
            }
        }
        return r;
    }

    private Animatable2DGroup _quickItemsGroup;
    private List<QuickItemHud> _quickItemSlots;

    #endregion
}