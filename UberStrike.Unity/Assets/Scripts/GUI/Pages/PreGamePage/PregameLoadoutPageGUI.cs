using System.Collections;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.Core.Types;
using UnityEngine;

public class PregameLoadoutPageGUI : PageGUI
{
    private void Awake()
    {
        _weaponDetailGui = new WeaponDetailGUI();
        _weaponRecomGui = new WeaponRecommendListGUI(BuyingLocationType.PreGame);
        _weaponRecomGui.OnSelectionChange = _weaponDetailGui.SetWeaponItem;
    }

    private void OnEnable()
    {
        OnUpdateRecommendationEvent(new UpdateRecommendationEvent());

        _weaponRecomGui.Enabled = true;
        CmuneEventHandler.AddListener<UpdateRecommendationEvent>(OnUpdateRecommendationEvent);
    }

    private void OnDisable()
    {
        _weaponRecomGui.Enabled = false;
        CmuneEventHandler.RemoveListener<UpdateRecommendationEvent>(OnUpdateRecommendationEvent);
    }

    private void OnUpdateRecommendationEvent(UpdateRecommendationEvent ev)
    {
        List<KeyValuePair<RecommendType, IUnityItem>> recommendations = new List<KeyValuePair<RecommendType, IUnityItem>>(3);
        recommendations.Add(new KeyValuePair<RecommendType, IUnityItem>(RecommendType.StaffPick, ItemManager.Instance.GetRecommendedItem(GameState.CurrentSpace.MapId)));

        var recommendation = RecommendationUtils.GetRecommendedWeapon(PlayerDataManager.PlayerLevelSecure, GameState.CurrentSpace.CombatRangeTiers);
        recommendations.Add(new KeyValuePair<RecommendType, IUnityItem>(RecommendType.RecommendedWeapon, recommendation.ItemWeapon ?? RecommendationUtils.FallBackWeapon));
        //RecommendationUtils.DebugRecommendation(recommendation);

        recommendations.Add(new KeyValuePair<RecommendType, IUnityItem>(RecommendType.RecommendedArmor, ShopUtils.GetRecommendedArmor(PlayerDataManager.PlayerLevelSecure, LoadoutManager.Instance.GetItemOnSlot<HoloGearItem>(LoadoutSlotType.GearHolo), LoadoutManager.Instance.GetItemOnSlot<GearItem>(LoadoutSlotType.GearUpperBody), LoadoutManager.Instance.GetItemOnSlot<GearItem>(LoadoutSlotType.GearLowerBody))));
        _weaponRecomGui.UpdateRecommendedList(recommendations);
    }

    public override void DrawGUI(Rect rect)
    {
        GUI.skin = BlueStonez.Skin;

        GUI.BeginGroup(rect);
        DrawPanel(rect);
        GUI.EndGroup();

        DoDragControls();

        _tooltip.OnGui();
    }

    public void EquipBoughtWeapon(IUnityItem baseItem)
    {
        if (baseItem == null)
        {
            return;
        }

        switch (_lastSelectedSlot)
        {
            case LoadoutSlotType.WeaponMelee:
            case LoadoutSlotType.WeaponPrimary:
            case LoadoutSlotType.WeaponSecondary:
            case LoadoutSlotType.WeaponTertiary:
                {
                    LoadoutManager.Instance.SetSlot(_lastSelectedSlot, baseItem);
                    LoadoutManager.Instance.EquipWeapon(_lastSelectedSlot, baseItem as WeaponItem);
                }
                break;
            default:
                Debug.LogError("Item not equipped because slot type not correct: " + _lastSelectedSlot);
                break;
        }
    }

    #region Private

    private void DrawPanel(Rect panelRect)
    {
        GUI.BeginGroup(new Rect(1, 0, panelRect.width - 2, panelRect.height));
        {
            DrawWeaponRecommend(new Rect(0, 0, panelRect.width - 2, 242));
            DrawPlayerLoadout(new Rect(0, 242 - 1, panelRect.width - 2, 167));
        }
        GUI.EndGroup();
    }

    private void DrawWeaponRecommend(Rect rect)
    {
        DrawRecommendContent(new Rect(0, 0, rect.width, rect.height));
    }

    private void DrawRecommendContent(Rect rect)
    {
        GUI.BeginGroup(rect);
        {
            _weaponDetailGui.Draw(new Rect(0, 0, 200, rect.height));
            _weaponRecomGui.Draw(new Rect(200 - 1, 0, rect.width - 200 + 1, rect.height));
        }
        GUI.EndGroup();
    }

    private void DrawPlayerLoadout(Rect rect)
    {
        GUI.BeginGroup(rect);
        {
            DrawQuickItemLoadout(new Rect(0, 5, rect.width * 0.2f, rect.height));
            DrawWeaponLoadout(new Rect(rect.width * 0.2f, 0, rect.width * 0.4f + 1, rect.height));
            DrawGearLoadout(new Rect(rect.width * 0.6f, 0, rect.width * 0.4f, rect.height));
        }
        GUI.EndGroup();
    }

    private void DrawQuickItemLoadout(Rect rect)
    {
        GUI.BeginGroup(rect);
        {
            DrawLoadoutItem(LocalizedStrings.QuickItem, LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.QuickUseItem1),
                   new Rect(rect.width / 2 - 24, rect.height / 2 - 80, 48, 48), LoadoutSlotType.QuickUseItem1,
                   string.Empty, true);
            DrawLoadoutItem(LocalizedStrings.QuickItem, LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.QuickUseItem2),
                   new Rect(rect.width / 2 - 24, rect.height / 2 - 24, 48, 48), LoadoutSlotType.QuickUseItem2,
                   string.Empty, true);
            DrawLoadoutItem(LocalizedStrings.QuickItem, LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.QuickUseItem3),
                   new Rect(rect.width / 2 - 24, rect.height / 2 + 32, 48, 48), LoadoutSlotType.QuickUseItem3,
                   string.Empty, true);
        }
        GUI.EndGroup();
    }

    private void DrawWeaponLoadout(Rect rect)
    {
        GUI.BeginGroup(rect);
        {
            DrawGroupControl(new Rect(0, 10, rect.width, rect.height - 10), "WEAPONS", BlueStonez.label_group_interparkbold_18pt);
            DrawWeaponLoadoutRangeIcon(new Rect(rect.width / 2 - 65, rect.height / 2 - 85, 128, 128));

            float gap = (rect.width - 48 * 4 - 12 * 2) / 3;
            DrawLoadoutItem(LocalizedStrings.Melee, LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.WeaponMelee),
                new Rect(12, rect.height - 56, 48, 48), LoadoutSlotType.WeaponMelee,
                InputManager.Instance.GetKeyAssignmentString(GameInputKey.WeaponMelee), true);
            DrawLoadoutItem(LocalizedStrings.PrimaryWeapon, LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.WeaponPrimary),
                new Rect(12 + 48 + gap, rect.height - 56, 48, 48), LoadoutSlotType.WeaponPrimary,
                InputManager.Instance.GetKeyAssignmentString(GameInputKey.Weapon1), true);
            DrawLoadoutItem(LocalizedStrings.SecondaryWeapon, LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.WeaponSecondary),
                new Rect(12 + (48 + gap) * 2, rect.height - 56, 48, 48), LoadoutSlotType.WeaponSecondary,
                InputManager.Instance.GetKeyAssignmentString(GameInputKey.Weapon2), true);
            DrawLoadoutItem(LocalizedStrings.TertiaryWeapon, LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.WeaponTertiary),
                new Rect(12 + (48 + gap) * 3, rect.height - 56, 48, 48), LoadoutSlotType.WeaponTertiary,
                InputManager.Instance.GetKeyAssignmentString(GameInputKey.Weapon3), true);
        }
        GUI.EndGroup();
    }

    private void DrawWeaponLoadoutRangeIcon(Rect rect)
    {
        CombatRangeIcon.Instance.DrawWeaponRangeIcon2(rect,
            LoadoutManager.Instance.GetItemOnSlot<WeaponItem>(LoadoutSlotType.WeaponMelee),
            LoadoutManager.Instance.GetItemOnSlot<WeaponItem>(LoadoutSlotType.WeaponPrimary),
            LoadoutManager.Instance.GetItemOnSlot<WeaponItem>(LoadoutSlotType.WeaponSecondary),
            LoadoutManager.Instance.GetItemOnSlot<WeaponItem>(LoadoutSlotType.WeaponTertiary));
        CombatRangeIcon.Instance.DrawRecommendedCombatRange(rect, GameState.CurrentSpace.CombatRangeTiers,
            LoadoutManager.Instance.GetItemOnSlot<WeaponItem>(LoadoutSlotType.WeaponMelee),
            LoadoutManager.Instance.GetItemOnSlot<WeaponItem>(LoadoutSlotType.WeaponPrimary),
            LoadoutManager.Instance.GetItemOnSlot<WeaponItem>(LoadoutSlotType.WeaponSecondary),
            LoadoutManager.Instance.GetItemOnSlot<WeaponItem>(LoadoutSlotType.WeaponTertiary));
    }

    private void DrawGearLoadout(Rect rect)
    {
        GUI.BeginGroup(rect);
        {
            DrawGroupControl(new Rect(0, 10, rect.width, rect.height - 10), "ARMOR", BlueStonez.label_group_interparkbold_18pt);
            float gap = (rect.width - 48 * 3 - 22 * 2) / 2;
            DrawLoadoutItem(LocalizedStrings.UpperBody, LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.GearUpperBody),
                new Rect(22, rect.height - 56, 48, 48), LoadoutSlotType.GearUpperBody, "UB", false);
            DrawLoadoutItem(LocalizedStrings.LowerBody, LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.GearLowerBody),
                new Rect(22 + 48 + gap, rect.height - 56, 48, 48), LoadoutSlotType.GearLowerBody, "LB", false);
            DrawLoadoutItem(LocalizedStrings.UpperBody, LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.GearHolo),
                new Rect(22 + (48 + gap) * 2, rect.height - 56, 48, 48), LoadoutSlotType.GearHolo, "HO", false);

            int armorPoint = 0;
            int absorptionPercent = 0;
            LoadoutManager.Instance.GetArmorValues(out armorPoint, out absorptionPercent);
            ArmorPowerIcon.Instance.DrawArmorPower(new Rect(rect.width / 2 - 45, rect.height / 2 - 70, 90, 90), armorPoint, absorptionPercent);
        }
        GUI.EndGroup();
    }

    private ItemToolTip _tooltip = new ItemToolTip();

    private void DrawLoadoutItem(string slotName, InventoryItem item,
        Rect rect, LoadoutSlotType loadoutSlotType, string slotTag, bool supportDrag)
    {
        if (item != null && item.Item != null)
        {
            GUI.Label(new Rect(rect.x, rect.y, 48, 48), new GUIContent(item.Item.Icon), BlueStonez.item_slot_large);

            if (new Rect(rect.x, rect.y, 48, 48).Contains(Event.current.mousePosition))
            {
                _tooltip.SetItem(item.Item, new Rect(rect.x, rect.y, 48, 48), PopupViewSide.Left);
            }
        }
        else
        {
            GUI.Label(new Rect(rect.x, rect.y, 48, 48), new GUIContent(string.Empty, LocalizedStrings.Empty), BlueStonez.item_slot_large);
        }

        if (!string.IsNullOrEmpty(slotTag)) DrawSlotTag(rect, slotTag);
        if (supportDrag)
        {
            WeaponEquipArea(rect, GUIContent.none, item.Item, loadoutSlotType, BlueStonez.loadoutdropslot);
        }
    }

    private void DrawSlotTag(Rect rect, string slotTag)
    {
        GUI.color = Color.black;
        GUI.Label(new Rect(rect.x + 3, rect.y + rect.height - 19, rect.width, 18), slotTag, BlueStonez.label_interparkbold_18pt_left);
        GUI.color = Color.white;
        GUI.Label(new Rect(rect.x + 2, rect.y + rect.height - 18, rect.width, 18), slotTag, BlueStonez.label_interparkbold_18pt_left);
    }

    #region Drag & Drop shit
    private bool WeaponEquipArea(Rect position, GUIContent guiContent, IUnityItem baseItem,
        LoadoutSlotType loadoutSlotType, GUIStyle guiStyle)
    {
        bool result = false;
        int id = GUIUtility.GetControlID(_itemSlotButtonHash, FocusType.Native);

        switch (Event.current.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                // If the mouse is inside the button, we say that we're the hot control
                if (position.Contains(Event.current.mousePosition) && !_isDropZoomAnimating)
                {
                    GUIUtility.hotControl = id;
                    Event.current.Use();
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == id && !_isDropZoomAnimating)
                {
                    GUIUtility.hotControl = 0;
                    // If we got the mousedown, the mouseup is ours as well (no matter if the click was in the button or not)
                    Event.current.Use();
                    // But we only return true if the button was actually clicked
                    result = position.Contains(Event.current.mousePosition);
                }
                else if (position.Contains(Event.current.mousePosition) && _activeDragItem != null)
                {
                    // If the dragged item not equipped
                    if (!_activeDragItemEquipped)
                    {
                        switch (_activeDragItemLoadoutSlot)
                        {
                            case LoadoutSlotType.Inventory:
                                {
                                    //We own the item, just equip it
                                    switch (loadoutSlotType)
                                    {
                                        case LoadoutSlotType.WeaponMelee:
                                            if (_activeDragItem.ItemClass == UberstrikeItemClass.WeaponMelee)
                                            {
                                                _lastSelectedSlot = LoadoutSlotType.WeaponMelee;
                                                LoadoutManager.Instance.SetSlot(loadoutSlotType, _activeDragItem);
                                            }
                                            break;
                                        case LoadoutSlotType.WeaponPrimary:
                                            if (_activeDragItem.ItemType == UberstrikeItemType.Weapon && _activeDragItem.ItemClass != UberstrikeItemClass.WeaponMelee)
                                            {
                                                _lastSelectedSlot = LoadoutSlotType.WeaponPrimary;
                                                LoadoutManager.Instance.SetSlot(loadoutSlotType, _activeDragItem);
                                            }
                                            break;
                                        case LoadoutSlotType.WeaponSecondary:
                                            if (_activeDragItem.ItemType == UberstrikeItemType.Weapon && _activeDragItem.ItemClass != UberstrikeItemClass.WeaponMelee)
                                            {
                                                _lastSelectedSlot = LoadoutSlotType.WeaponSecondary;
                                                LoadoutManager.Instance.SetSlot(loadoutSlotType, _activeDragItem);
                                            }
                                            break;
                                        case LoadoutSlotType.WeaponTertiary:
                                            if (_activeDragItem.ItemType == UberstrikeItemType.Weapon && _activeDragItem.ItemClass != UberstrikeItemClass.WeaponMelee)
                                            {
                                                _lastSelectedSlot = LoadoutSlotType.WeaponTertiary;
                                                LoadoutManager.Instance.SetSlot(loadoutSlotType, _activeDragItem);
                                            }
                                            break;
                                        case LoadoutSlotType.QuickUseItem1:
                                        case LoadoutSlotType.QuickUseItem2:
                                        case LoadoutSlotType.QuickUseItem3:
                                            if (_activeDragItem.ItemType == UberstrikeItemType.QuickUse)
                                            {
                                                LoadoutManager.Instance.SetSlot(loadoutSlotType, _activeDragItem);
                                            }
                                            break;
                                    }
                                }
                                break;
                        }
                    }
                    else
                    {
                        switch (loadoutSlotType)
                        {
                            case LoadoutSlotType.WeaponMelee:
                                LoadoutManager.Instance.SwitchWeaponsInLoadout(0, _weaponTakenFromSlot);
                                break;
                            case LoadoutSlotType.WeaponPrimary:
                                LoadoutManager.Instance.SwitchWeaponsInLoadout(1, _weaponTakenFromSlot);
                                break;
                            case LoadoutSlotType.WeaponSecondary:
                                LoadoutManager.Instance.SwitchWeaponsInLoadout(2, _weaponTakenFromSlot);
                                break;
                            case LoadoutSlotType.WeaponTertiary:
                                LoadoutManager.Instance.SwitchWeaponsInLoadout(3, _weaponTakenFromSlot);
                                break;
                            case LoadoutSlotType.QuickUseItem1:
                                LoadoutManager.Instance.SwitchQuickItemInLoadout(1, _weaponTakenFromSlot);
                                break;
                            case LoadoutSlotType.QuickUseItem2:
                                LoadoutManager.Instance.SwitchQuickItemInLoadout(2, _weaponTakenFromSlot);
                                break;
                            case LoadoutSlotType.QuickUseItem3:
                                LoadoutManager.Instance.SwitchQuickItemInLoadout(3, _weaponTakenFromSlot);
                                break;
                            default:
                                break;
                        }
                        _weaponTakenFromSlot = -1;
                    }
                }
                break;
            case EventType.MouseDrag:
                //Check if this control is active and if it's draggable
                if (GUIUtility.hotControl == id && !_isDropZoomAnimating /*&& _selectedItem == buttonID*/)
                {
                    //This control is being dragged, setup the ActiveDragItem and DraggedControlID
                    _draggedControlID = GUIUtility.hotControl;
                    Vector2 controlScreenPos = GUIUtility.GUIToScreenPoint(new Vector2(position.x, position.y));
                    _draggedControlRect = new Rect(controlScreenPos.x, controlScreenPos.y, position.width, position.height);

                    _activeDragItem = baseItem;
                    _activeDragItemEquipped = true;
                    _activeDragItemLoadoutSlot = loadoutSlotType;
                    if (baseItem != null)
                    {
                        switch (loadoutSlotType)
                        {
                            case LoadoutSlotType.WeaponMelee:
                                _weaponTakenFromSlot = 0;
                                break;
                            case LoadoutSlotType.WeaponPrimary:
                                _weaponTakenFromSlot = 1;
                                break;
                            case LoadoutSlotType.WeaponSecondary:
                                _weaponTakenFromSlot = 2;
                                break;
                            case LoadoutSlotType.WeaponTertiary:
                                _weaponTakenFromSlot = 3;
                                break;
                            case LoadoutSlotType.QuickUseItem1:
                                _weaponTakenFromSlot = 1;
                                break;
                            case LoadoutSlotType.QuickUseItem2:
                                _weaponTakenFromSlot = 2;
                                break;
                            case LoadoutSlotType.QuickUseItem3:
                                _weaponTakenFromSlot = 3;
                                break;
                            default:
                                _weaponTakenFromSlot = -1;
                                break;
                        }
                    }
                    GUIUtility.hotControl = 0;
                    Event.current.Use();
                }
                break;
            case EventType.Repaint:
                guiStyle.Draw(position, guiContent, id);
                break;
        }
        return result;
    }

    private void DrawGroupControl(Rect rect, string title, GUIStyle textStyle)
    {
        GUI.BeginGroup(rect, string.Empty, BlueStonez.group_grey81);
        GUI.EndGroup();
        GUI.Label(new Rect(rect.x + 18, rect.y - 8, textStyle.CalcSize(new GUIContent(title)).x + 10, 16), title, textStyle);
    }

    private void DoDragControls()
    {
        // If MouseUp anywhere, turn off the drag mode & reset the ActiveDragItem
        if (Event.current.type == EventType.MouseUp)
        {
            _draggedControlID = 0;
            _doDragZoom = false;
            _activeDragItem = null;
            _activeDragItemEquipped = false;
            _activeDragItemLoadoutSlot = LoadoutSlotType.Shop;
        }

        //If we are dragging, create an visual GUIItem to follow the mouse
        if (_draggedControlID > 0 && _activeDragItem != null)
        {
            if (!_doDragZoom)
            {
                _doDragZoom = true;
                StartCoroutine(StartDragZoom(0, 1, 1.25f, 0.1f, 0.8f));
            }
            else
            {
                if (!_showDragZoomAnimation)
                {
                    Vector2 currentMousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                    currentMousePos = GUIUtility.ScreenToGUIPoint(currentMousePos);
                    _dragScalePivot = new Vector2(currentMousePos.x, currentMousePos.y);
                }

                GUIUtility.ScaleAroundPivot(new Vector2(_zoomMultiplier, _zoomMultiplier), _dragScalePivot);
                GUI.backgroundColor = new Color(1, 1, 1, _alphaValue);
                GUI.Label(new Rect(_dragScalePivot.x - 24, _dragScalePivot.y - 24, 48, 48), _activeDragItem.Icon, BlueStonez.item_slot_large);
            }
        }
        else
        {
            //The Item has been dropped, lets animate
            if (_dropZoomAnimating)
            {
                GUI.color = new Color(1, 1, 1, _alphaValue);
                GUI.Label(new Rect(_draggedControlRect.xMin, _draggedControlRect.yMin, 48, 48), _activeDragItem.Icon, BlueStonez.item_slot_large);
                GUIUtility.ScaleAroundPivot(new Vector2(_zoomMultiplier, _zoomMultiplier), new Vector2(_dropTargetPositon.x + 32, _dropTargetPositon.y + 32));
                GUI.Label(new Rect(_dropTargetPositon.x, _dropTargetPositon.y, 48, 48), _activeDragItem.Icon, BlueStonez.item_slot_large);
            }
        }
    }

    private IEnumerator StartDragZoom(float startTime, float startZoom, float endZoom,
        float startAlpha, float endAlpha)
    {
        _showDragZoomAnimation = true;
        float time = 0f;
        float DragScalePivotMultiplierX = 0;
        float DragScalePivotMultiplierY = 0;
        _dragScalePivot = new Vector2(_draggedControlRect.xMin + 32, _draggedControlRect.yMin + 32);

        while (time < startTime)
        {
            _alphaValue = Mathf.Lerp(startAlpha, endAlpha, time / startTime);
            _zoomMultiplier = Mathfx.Berp(startZoom, endZoom, time / startTime);
            Vector2 currentMousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            currentMousePos = GUIUtility.ScreenToGUIPoint(currentMousePos);
            DragScalePivotMultiplierX = Mathf.Lerp(_draggedControlRect.xMin + 32, currentMousePos.x, time / startTime);
            DragScalePivotMultiplierY = Mathf.Lerp(_draggedControlRect.yMin + 32, currentMousePos.y, time / startTime);
            _dragScalePivot = new Vector2(DragScalePivotMultiplierX, DragScalePivotMultiplierY);
            time += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        _alphaValue = endAlpha;
        _zoomMultiplier = endZoom;
        _showDragZoomAnimation = false;
    }
    #endregion

    private string ModeName
    {
        get
        {
            return GameModes.GetModeName(GameState.CurrentGameMode);
        }
    }

    private WeaponDetailGUI _weaponDetailGui;
    private WeaponRecommendListGUI _weaponRecomGui;

    private static int _itemSlotButtonHash = "Button".GetHashCode();
    private bool _doDragZoom = false;
    private float _zoomMultiplier = 1.0f;
    private bool _isDropZoomAnimating = false;
    private bool _showDragZoomAnimation = false;
    private bool _dropZoomAnimating = false;
    private float _alphaValue = 1.0f;
    private IUnityItem _activeDragItem;
    private bool _activeDragItemEquipped = false;
    private LoadoutSlotType _activeDragItemLoadoutSlot;
    private int _draggedControlID;
    private Rect _draggedControlRect;
    private LoadoutSlotType _lastSelectedSlot = LoadoutSlotType.Inventory;
    private int _weaponTakenFromSlot = -1;
    private Vector2 _dragScalePivot;
    private Vector2 _dropTargetPositon = Vector2.zero;

    #endregion
}