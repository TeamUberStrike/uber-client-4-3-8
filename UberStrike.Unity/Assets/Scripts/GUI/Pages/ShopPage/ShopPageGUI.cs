using System.Collections;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.Core.Types;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;

public class ShopPageGUI : PageGUI
{
    #region Fields

    private const int SlotHeight = 70;
    private const int LoadoutWidth = 190;

    MasBundleShopGUI _masBundleGui = new MasBundleShopGUI();
    MasCreditsShopGui _masCreditsGui = new MasCreditsShopGui();
    LotteryShopGUI _lotteryGui = new LotteryShopGUI();

    ShopSorting.ItemComparer<BaseItemGUI> _inventoryComparer = new ShopSorting.DurationComparer();
    ShopSorting.ItemComparer<BaseItemGUI> _shopComparer = new ShopSorting.PriceComparer();

    private ItemToolTip _itemTooltip = new ItemToolTip();

    private bool _firstLogin = false;

    private SelectionGroup<ShopArea> _shopAreaSelection = new SelectionGroup<ShopArea>();
    private SelectionGroup<LoadoutArea> _loadoutAreaSelection = new SelectionGroup<LoadoutArea>();
    private SelectionGroup<UberstrikeItemType> _typeSelection = new SelectionGroup<UberstrikeItemType>();
    private SelectionGroup<UberstrikeItemClass> _weaponClassSelection = new SelectionGroup<UberstrikeItemClass>();
    private SelectionGroup<UberstrikeItemClass> _gearClassSelection = new SelectionGroup<UberstrikeItemClass>();

    private Rect _rectLabs;
    private Rect _shopArea;
    private Rect _loadoutArea;

    private Vector2 _loadoutWeaponScroll;
    private Vector2 _loadoutGearScroll;
    private Vector2 _loadoutQuickUseFuncScroll;
    private Vector2 _labScroll;

    private float _highlightedSlotAlpha = 0.2f;
    private LoadoutSlotType _highlightedSlot = LoadoutSlotType.None;

    private List<Rect> _activeLoadoutUsedSpace = new List<Rect>();

    private Dictionary<LoadoutSlotType, bool> _renewItem = new Dictionary<LoadoutSlotType, bool>();

    private bool _showRenewLoadoutButton = false;
    private int _skippedDefaultGearCount = 0;

    private float shopPositionX = 0;

    private List<BaseItemGUI> _shopItemGUIList = new List<BaseItemGUI>();
    private List<BaseItemGUI> _inventoryItemGUIList = new List<BaseItemGUI>();

    private IShopItemFilter _itemFilter;

    private SearchBarGUI _searchBar;

    [SerializeField]
    private BuyingLocationType _location;

    #endregion Fields

    private void Awake()
    {
        _itemFilter = new SpecialItemFilter();

        _firstLogin = true;
        IsOnGUIEnabled = true;

        _searchBar = new SearchBarGUI("SearchInShop");
    }

    private void Start()
    {
        // LOADOUT
        _loadoutAreaSelection.Add(LoadoutArea.Weapons, new GUIContent(UberstrikeIcons.LoadoutWeapon));
        _loadoutAreaSelection.Add(LoadoutArea.Gear, new GUIContent(UberstrikeIcons.LoadoutGear));
        _loadoutAreaSelection.Add(LoadoutArea.QuickItems, new GUIContent(UberstrikeIcons.LoadoutQuick));
        _loadoutAreaSelection.OnSelectionChange += SelectLoadoutArea;

        // SHOP AREA
        _shopAreaSelection.Add(ShopArea.Inventory, new GUIContent(string.Empty, UberstrikeIcons.LabInventory, "Inventory"));
        _shopAreaSelection.Add(ShopArea.Shop, new GUIContent(string.Empty, UberstrikeIcons.LabShop, "Shop"));
        _shopAreaSelection.Add(ShopArea.MysteryBox, new GUIContent(string.Empty, UberstrikeTextures.IconLottery, "Mystery Box"));
        if (Application.isEditor || ApplicationDataManager.Channel == ChannelType.MacAppStore)
        {
            _shopAreaSelection.Add(ShopArea.Packs, new GUIContent(string.Empty, UberstrikeIcons.LabPacks, "Packs"));
            _shopAreaSelection.Add(ShopArea.Credits, new GUIContent(string.Empty, UberstrikeIcons.LabCredits, "Credits"));
        }
        _shopAreaSelection.OnSelectionChange += (area) =>
        {
            UpdateItemFilter();
        };

        // TYPE
        _typeSelection.Add(UberstrikeItemType.Special, new GUIContent(UberstrikeIcons.ItemNewHotSale, LocalizedStrings.NewAndSaleItems));
        _typeSelection.Add(UberstrikeItemType.Weapon, new GUIContent(UberstrikeIcons.ItemWeapons, LocalizedStrings.Weapons));
        _typeSelection.Add(UberstrikeItemType.Gear, new GUIContent(UberstrikeIcons.ItemGear, LocalizedStrings.Gear));
        _typeSelection.Add(UberstrikeItemType.QuickUse, new GUIContent(UberstrikeIcons.ItemQuick, LocalizedStrings.QuickItems));
        _typeSelection.Add(UberstrikeItemType.Functional, new GUIContent(UberstrikeIcons.ItemFunctional, LocalizedStrings.FunctionalItems));
        _typeSelection.OnSelectionChange += (itemType) =>
        {
            UpdateItemFilter();
        };

        // WEAPONS
        _weaponClassSelection.Add(UberstrikeItemClass.WeaponMelee, new GUIContent(UberstrikeIcons.WeaponMelee, LocalizedStrings.MeleeWeapons));
        _weaponClassSelection.Add(UberstrikeItemClass.WeaponHandgun, new GUIContent(UberstrikeIcons.WeaponHandGun, LocalizedStrings.Handguns));
        _weaponClassSelection.Add(UberstrikeItemClass.WeaponMachinegun, new GUIContent(UberstrikeIcons.WeaponMachineGun, LocalizedStrings.Machineguns));
        _weaponClassSelection.Add(UberstrikeItemClass.WeaponShotgun, new GUIContent(UberstrikeIcons.WeaponShotGun, LocalizedStrings.Shotguns));
        _weaponClassSelection.Add(UberstrikeItemClass.WeaponSniperRifle, new GUIContent(UberstrikeIcons.WeaponSniperRifle, LocalizedStrings.SniperRifles));
        _weaponClassSelection.Add(UberstrikeItemClass.WeaponCannon, new GUIContent(UberstrikeIcons.WeaponCannon, LocalizedStrings.Cannons));
        _weaponClassSelection.Add(UberstrikeItemClass.WeaponSplattergun, new GUIContent(UberstrikeIcons.WeaponSplatterGun, LocalizedStrings.Splatterguns));
        _weaponClassSelection.Add(UberstrikeItemClass.WeaponLauncher, new GUIContent(UberstrikeIcons.WeaponLauncher, LocalizedStrings.Launchers));
        _weaponClassSelection.OnSelectionChange += (itemClass) =>
        {
            UpdateItemFilter();
        };

        // GEAR
        _gearClassSelection.Add(UberstrikeItemClass.GearBoots, new GUIContent(UberstrikeIcons.GearBoots, LocalizedStrings.Boots));
        _gearClassSelection.Add(UberstrikeItemClass.GearHead, new GUIContent(UberstrikeIcons.GearHead, LocalizedStrings.Head));
        _gearClassSelection.Add(UberstrikeItemClass.GearFace, new GUIContent(UberstrikeIcons.GearFace, LocalizedStrings.Face));
        _gearClassSelection.Add(UberstrikeItemClass.GearUpperBody, new GUIContent(UberstrikeIcons.GearUpperBody, LocalizedStrings.UpperBody));
        _gearClassSelection.Add(UberstrikeItemClass.GearLowerBody, new GUIContent(UberstrikeIcons.GearLowerBody, LocalizedStrings.LowerBody));
        _gearClassSelection.Add(UberstrikeItemClass.GearGloves, new GUIContent(UberstrikeIcons.GearGloves, LocalizedStrings.Gloves));
        _gearClassSelection.Add(UberstrikeItemClass.GearHolo, new GUIContent(UberstrikeIcons.GearHolo, LocalizedStrings.Holo));
        _gearClassSelection.OnSelectionChange += (itemClass) =>
        {
            UpdateItemFilter();
        };

        if (_showRenewLoadoutButton)
        {
            foreach (LoadoutSlotType slot in LoadoutManager.WeaponSlots)
            {
                InventoryItem item;
                if (LoadoutManager.Instance.TryGetItemInSlot(slot, out item))
                    _renewItem[slot] = !InventoryManager.Instance.IsItemValidForDays(item, 5);
            }

            foreach (LoadoutSlotType slot in LoadoutManager.GearSlots)
            {
                InventoryItem item;
                if (LoadoutManager.Instance.TryGetItemInSlot(slot, out item))
                    _renewItem[slot] = !InventoryManager.Instance.IsItemValidForDays(item, 5);
            }
        }

        UpdateShopItems();

        //only select the shop if we havn't already selected a specific area
        if (_shopAreaSelection.Index == 0)
            _shopAreaSelection.Select(ShopArea.Shop);

        _typeSelection.Select(UberstrikeItemType.Special);
        _gearClassSelection.SetIndex(-1);
        _weaponClassSelection.SetIndex(-1);
    }

    private void OnEnable()
    {
        CmuneEventHandler.AddListener<SelectShopAreaEvent>(OnSelectShopAreaEvent);
        CmuneEventHandler.AddListener<SelectLoadoutAreaEvent>(OnSelectLoadoutAreaEvent);
        CmuneEventHandler.AddListener<ShopHighlightSlotEvent>(OnHighlightSlotEvent);
        CmuneEventHandler.AddListener<ShopRefreshCurrentItemListEvent>(OnRefreshCurrentItemListEvent);

        DragAndDrop.Instance.OnDragBegin += OnBeginDrag;

        TemporaryLoadoutManager.Instance.ResetGearLoadout();
        AvatarAnimationManager.Instance.ResetAnimationState(PageType.Shop);

        StartCoroutine(StartNotifyLoadoutArea());

        if (IsOnGUIEnabled)
        {
            StartCoroutine(ScrollShopFromRight(0.25f));
        }

        _searchBar.ClearFilter();
    }

    private void OnDisable()
    {
        CmuneEventHandler.RemoveListener<SelectShopAreaEvent>(OnSelectShopAreaEvent);
        CmuneEventHandler.RemoveListener<SelectLoadoutAreaEvent>(OnSelectLoadoutAreaEvent);
        CmuneEventHandler.RemoveListener<ShopRefreshCurrentItemListEvent>(OnRefreshCurrentItemListEvent);
        CmuneEventHandler.RemoveListener<ShopHighlightSlotEvent>(OnHighlightSlotEvent);

        DragAndDrop.Instance.OnDragBegin -= OnBeginDrag;
    }

    #region Event Handler

    private void OnHighlightSlotEvent(ShopHighlightSlotEvent ev)
    {
        HighlightingSlot(ev.SlotType);
    }

    private void OnSelectShopAreaEvent(SelectShopAreaEvent ev)
    {
        _shopAreaSelection.Select(ev.ShopArea);

        if (ev.ItemType != 0)
            _typeSelection.Select(ev.ItemType);

        if (ev.ItemClass != 0)
        {
            switch (ev.ItemType)
            {
                case UberstrikeItemType.Gear:
                    _gearClassSelection.Select(ev.ItemClass);
                    break;
                case UberstrikeItemType.Weapon:
                    _weaponClassSelection.Select(ev.ItemClass);
                    break;
            }
        }
    }

    private void OnSelectLoadoutAreaEvent(SelectLoadoutAreaEvent ev)
    {
        _loadoutAreaSelection.Select(ev.Area);
    }

    #endregion

    private IEnumerator ScrollShopFromRight(float time)
    {
        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;
            shopPositionX = Mathf.Lerp(0, 590, (t / time) * (t / time));
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator StartNotifyLoadoutArea()
    {
        yield return new WaitForEndOfFrame();

        CmuneEventHandler.Route(new LoadoutAreaChangedEvent() { Area = _loadoutAreaSelection.Current });
    }

    private void OnGUI()
    {
        if (IsOnGUIEnabled)
        {
            DrawGUI(new Rect(Screen.width - shopPositionX, GlobalUIRibbon.Instance.GetHeight(), 590,
                Screen.height - GlobalUIRibbon.Instance.GetHeight() + 1));

            ArmorHud.Instance.Update();
        }
    }

    public override void DrawGUI(Rect rect)
    {
        GUI.depth = (int)GuiDepth.Page;
        GUI.skin = BlueStonez.Skin;

        if (_firstLogin)
        {
            _firstLogin = false;
        }

        _rectLabs = rect;
        _rectLabs.width += 10.0f;

        GUITools.PushGUIState();

        GUI.enabled = !PopupSystem.IsAnyPopupOpen && !PanelManager.IsAnyPanelOpen;

        GUI.BeginGroup(_rectLabs);
        {
            // Draw the Loadout Panel
            DrawLoadout(new Rect(0, 0, LoadoutWidth, _rectLabs.height));

            // draw the labs panel
            DrawShop(new Rect(LoadoutWidth, 0, _rectLabs.width - 200, _rectLabs.height));
        }
        GUI.EndGroup();

        DragAndDrop.Instance.DrawSlot<ShopDragSlot>(new Rect(0, 55, Screen.width - 580, Screen.height - 55), OnDropAvatar);
        DragAndDrop.Instance.DrawSlot<ShopDragSlot>(new Rect(Screen.width - _rectLabs.width + 200, 55, _rectLabs.width - 200, Screen.height - 55), OnDropShop);

        GUITools.PopGUIState();

        if (!PopupSystem.IsAnyPopupOpen && !PanelManager.IsAnyPanelOpen)
        {
            Rect itemArea = new Rect(rect.x, rect.y + 72, rect.width, rect.height - 72);
            if (itemArea.Contains(Event.current.mousePosition))
            {
                _itemTooltip.OnGui();
            }

            GuiManager.DrawTooltip();
        }

        if (_highlightedSlotAlpha > 0)
        {
            _highlightedSlotAlpha = Mathf.Max(_highlightedSlotAlpha - Time.deltaTime * 0.5f, 0);
        }
    }

    public void EquipItemFromArea(IUnityItem item, LoadoutSlotType slot, ShopArea area)
    {
        if (item != null && !LoadoutManager.Instance.IsItemEquipped(item.ItemId))
        {
            if (InventoryManager.Instance.IsItemInInventory(item.ItemId))
            {
                InventoryManager.Instance.EquipItemOnSlot(item.ItemId, slot);
            }
            else
            {
                BuyPanelGUI panel = PanelManager.Instance.OpenPanel(PanelType.BuyItem) as BuyPanelGUI;
                if (panel)
                {
                    panel.SetItem(item, _location, BuyingRecommendationType.None, true);
                }
            }
        }
        else
        {
            Debug.LogError("Item is null or already equipped!");
        }
    }

    public void SelectLoadoutWeapon(LoadoutSlotType slot)
    {
        if (InventoryManager.Instance.CurrentWeaponSlot != slot)
        {
            InventoryManager.Instance.CurrentWeaponSlot = slot;

            GameState.LocalDecorator.SetActiveWeaponSlot(slot);
            GameState.LocalDecorator.ShowWeapon(slot);

            InventoryItem item;
            if (LoadoutManager.Instance.TryGetItemInSlot(slot, out item))
            {
                AvatarAnimationManager.Instance.SetAnimationState(PageType.Shop, item.Item.ItemClass);
            }
            else
            {
                AvatarAnimationManager.Instance.SetAnimationState(PageType.Shop, 0);
            }
        }
    }

    private void UnequipItem(IUnityItem item)
    {
        LoadoutSlotType slot;
        if (item != null && LoadoutManager.Instance.TryGetSlotForItem(item.ItemId, out slot))
        {
            UnequipItem(slot);
        }
    }

    private void UnequipItem(LoadoutSlotType slotType)
    {
        UberstrikeItemClass gearItem = 0;

        switch (slotType)
        {
            case LoadoutSlotType.GearHead:
                InventoryManager.Instance.EquipItemOnSlot(UberStrikeCommonConfig.DefaultHead, LoadoutSlotType.GearHead);
                gearItem = UberstrikeItemClass.GearHead;
                break;
            case LoadoutSlotType.GearFace:
                LoadoutManager.Instance.ResetSlot(LoadoutSlotType.GearFace);
                TemporaryLoadoutManager.Instance.SetGearLoadout(LoadoutSlotType.GearFace, null);
                SfxManager.Play2dAudioClip(SoundEffectType.UIEquipGear);
                if (GameState.LocalDecorator)
                    GameState.LocalDecorator.HideWeapons();
                gearItem = UberstrikeItemClass.GearFace;
                break;
            case LoadoutSlotType.GearGloves:
                InventoryManager.Instance.EquipItemOnSlot(UberStrikeCommonConfig.DefaultGloves, LoadoutSlotType.GearGloves);
                gearItem = UberstrikeItemClass.GearGloves;
                break;
            case LoadoutSlotType.GearUpperBody:
                InventoryManager.Instance.EquipItemOnSlot(UberStrikeCommonConfig.DefaultUpperBody, LoadoutSlotType.GearUpperBody);
                gearItem = UberstrikeItemClass.GearUpperBody;
                break;
            case LoadoutSlotType.GearLowerBody:
                InventoryManager.Instance.EquipItemOnSlot(UberStrikeCommonConfig.DefaultLowerBody, LoadoutSlotType.GearLowerBody);
                gearItem = UberstrikeItemClass.GearLowerBody;
                break;
            case LoadoutSlotType.GearBoots:
                InventoryManager.Instance.EquipItemOnSlot(UberStrikeCommonConfig.DefaultBoots, LoadoutSlotType.GearBoots);
                gearItem = UberstrikeItemClass.GearBoots;
                break;
            case LoadoutSlotType.GearHolo:
                LoadoutManager.Instance.ResetSlot(LoadoutSlotType.GearHolo);
                TemporaryLoadoutManager.Instance.SetGearLoadout(LoadoutSlotType.GearHolo, null);
                SfxManager.Play2dAudioClip(SoundEffectType.UIEquipGear);
                if (GameState.LocalDecorator)
                    GameState.LocalDecorator.HideWeapons();
                gearItem = UberstrikeItemClass.GearHolo;
                break;
            case LoadoutSlotType.WeaponMelee:
                LoadoutManager.Instance.ResetSlot(LoadoutSlotType.WeaponMelee);
                GameState.LocalDecorator.SetActiveWeaponSlot(slotType);
                GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponMelee, null);
                break;
            case LoadoutSlotType.WeaponPrimary:
                LoadoutManager.Instance.ResetSlot(LoadoutSlotType.WeaponPrimary);
                GameState.LocalDecorator.SetActiveWeaponSlot(slotType);
                GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponPrimary, null);
                break;
            case LoadoutSlotType.WeaponSecondary:
                LoadoutManager.Instance.ResetSlot(LoadoutSlotType.WeaponSecondary);
                GameState.LocalDecorator.SetActiveWeaponSlot(slotType);
                GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponSecondary, null);
                break;
            case LoadoutSlotType.WeaponTertiary:
                LoadoutManager.Instance.ResetSlot(LoadoutSlotType.WeaponTertiary);
                GameState.LocalDecorator.SetActiveWeaponSlot(slotType);
                GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponTertiary, null);
                break;
            case LoadoutSlotType.FunctionalItem1:
                LoadoutManager.Instance.ResetSlot(LoadoutSlotType.FunctionalItem1);
                break;
            case LoadoutSlotType.FunctionalItem2:
                LoadoutManager.Instance.ResetSlot(LoadoutSlotType.FunctionalItem2);
                break;
            case LoadoutSlotType.FunctionalItem3:
                LoadoutManager.Instance.ResetSlot(LoadoutSlotType.FunctionalItem3);
                break;
            case LoadoutSlotType.QuickUseItem1:
                LoadoutManager.Instance.ResetSlot(LoadoutSlotType.QuickUseItem1);
                break;
            case LoadoutSlotType.QuickUseItem2:
                LoadoutManager.Instance.ResetSlot(LoadoutSlotType.QuickUseItem2);
                break;
            case LoadoutSlotType.QuickUseItem3:
                LoadoutManager.Instance.ResetSlot(LoadoutSlotType.QuickUseItem3);
                break;
        }

        if (gearItem != 0) //is gear
        {
            AvatarAnimationManager.Instance.SetAnimationState(PageType.Shop, gearItem, slotType == LoadoutSlotType.GearHolo);
        }
        else
        {
            AvatarAnimationManager.Instance.SetAnimationState(PageType.Shop, 0);
        }

        HighlightingSlot(slotType);
    }

    /// <summary>
    /// set list of active spaces in current selected loadout 
    /// </summary>
    /// <param name="slots"></param>
    /// <param name="width"></param>
    private void SetActiveLoadoutActiveSpaces(int slots, float width)
    {
        _activeLoadoutUsedSpace.Clear();
        for (int i = 0; i < slots; i++)
        {
            _activeLoadoutUsedSpace.Add(new Rect(0, i * SlotHeight, width - 5, SlotHeight));
        }
    }

    private void SetActiveLoadoutActiveSpaces(params Rect[] rects)
    { // set list of active spaces in current selected loadout 
        _activeLoadoutUsedSpace.Clear();
        for (int i = 0; i < rects.Length; i++)
        {
            _activeLoadoutUsedSpace.Add(rects[i]);
        }
    }

    private bool IsMouseCursorInLoadout(Vector2 pos)
    {
        for (int i = 0; i < _activeLoadoutUsedSpace.Count; i++)
        {
            if (_activeLoadoutUsedSpace[i].Contains(pos))
            {
                return true;
            }
        }

        return false;
    }

    private void ExchangedWeaponLoadoutData(int firstSlot, int secondSlot)
    {
        switch (firstSlot)
        {
            case 1:
                if (LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.WeaponPrimary))
                    LoadoutManager.Instance.EquipWeapon(LoadoutSlotType.WeaponPrimary, LoadoutManager.Instance.GetItemOnSlot<WeaponItem>(LoadoutSlotType.WeaponPrimary));
                else
                    GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponPrimary, null);
                break;
            case 2:
                if (LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.WeaponSecondary))
                    LoadoutManager.Instance.EquipWeapon(LoadoutSlotType.WeaponSecondary, LoadoutManager.Instance.GetItemOnSlot<WeaponItem>(LoadoutSlotType.WeaponSecondary));
                else
                    GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponSecondary, null);
                break;
            case 3:
                if (LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.WeaponTertiary))
                    LoadoutManager.Instance.EquipWeapon(LoadoutSlotType.WeaponTertiary, LoadoutManager.Instance.GetItemOnSlot<WeaponItem>(LoadoutSlotType.WeaponTertiary));
                else
                    GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponTertiary, null);
                break;
        }
        switch (secondSlot)
        {
            case 1:
                if (LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.WeaponPrimary))
                    LoadoutManager.Instance.EquipWeapon(LoadoutSlotType.WeaponPrimary, LoadoutManager.Instance.GetItemOnSlot<WeaponItem>(LoadoutSlotType.WeaponPrimary));
                else
                    GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponPrimary, null);
                GameState.LocalDecorator.SetActiveWeaponSlot(LoadoutSlotType.WeaponPrimary);
                break;
            case 2:
                if (LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.WeaponSecondary))
                    LoadoutManager.Instance.EquipWeapon(LoadoutSlotType.WeaponSecondary, LoadoutManager.Instance.GetItemOnSlot<WeaponItem>(LoadoutSlotType.WeaponSecondary));
                else
                    GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponSecondary, null);
                GameState.LocalDecorator.SetActiveWeaponSlot(LoadoutSlotType.WeaponSecondary);
                break;
            case 3:
                if (LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.WeaponTertiary))
                    LoadoutManager.Instance.EquipWeapon(LoadoutSlotType.WeaponTertiary, LoadoutManager.Instance.GetItemOnSlot<WeaponItem>(LoadoutSlotType.WeaponTertiary));
                else
                    GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponTertiary, null);
                GameState.LocalDecorator.SetActiveWeaponSlot(LoadoutSlotType.WeaponTertiary);
                break;
        }
    }

    public void HighlightingSlot(LoadoutSlotType slot)
    {
        _highlightedSlot = slot;
        _highlightedSlotAlpha = 0.5f;

        //open correct loadout to show the highlighted area
        switch (slot)
        {
            case LoadoutSlotType.FunctionalItem1:
            case LoadoutSlotType.FunctionalItem2:
            case LoadoutSlotType.FunctionalItem3:
            case LoadoutSlotType.QuickUseItem1:
            case LoadoutSlotType.QuickUseItem2:
            case LoadoutSlotType.QuickUseItem3:
                SelectLoadoutArea(LoadoutArea.QuickItems); break;
            case LoadoutSlotType.WeaponMelee:
            case LoadoutSlotType.WeaponPrimary:
            case LoadoutSlotType.WeaponSecondary:
            case LoadoutSlotType.WeaponTertiary:
                SelectLoadoutArea(LoadoutArea.Weapons); break;
            default:
                SelectLoadoutArea(LoadoutArea.Gear); break;
        }
    }

    public void SelectLoadoutArea(LoadoutArea area)
    {
        switch (area)
        {
            case LoadoutArea.Gear:
            case LoadoutArea.QuickItems:
                SetActiveLoadoutActiveSpaces(6, LoadoutWidth - 5);
                break;
            case LoadoutArea.Weapons:
                SetActiveLoadoutActiveSpaces(4, LoadoutWidth - 5);
                break;
        }

        CmuneEventHandler.Route(new LoadoutAreaChangedEvent() { Area = area });
    }

    private void UpdateShopItems()
    {
        _shopItemGUIList.Clear();
        _inventoryItemGUIList.Clear();

        foreach (var item in InventoryManager.Instance.GetAllItems(false))
        {
            _inventoryItemGUIList.Add(new InventoryItemGUI(item, _location));
        }
        _inventoryItemGUIList.Sort(_inventoryComparer);

        foreach (var item in ItemManager.Instance.GetShopItems())
        {
            if (item.ItemView.IsConsumable)
            {
                _shopItemGUIList.Add(new ShopConsumableItemGUI(item, _location));
            }
            else
            {
                _shopItemGUIList.Add(new ShopRentItemGUI(item, _location));
            }
        }

        _shopItemGUIList.Sort(_shopComparer);
    }

    private void OnRefreshCurrentItemListEvent(ShopRefreshCurrentItemListEvent ev)
    {
        UpdateShopItems();
    }

    #region Labs GUI

    private void DrawLoadout(Rect rect)
    {
        _loadoutArea = rect;
        _loadoutArea.x += _rectLabs.x;
        _loadoutArea.y += _rectLabs.y;

        GUI.BeginGroup(rect, string.Empty, BlueStonez.window);
        {
            GUI.Box(new Rect(0, 0, rect.width - 1, rect.height), string.Empty, BlueStonez.window);
            GUI.Label(new Rect(0, 0, rect.width, 72), LocalizedStrings.LoadoutCaps, BlueStonez.tab_strip_large);

            int area = GUI.SelectionGrid(new Rect(4, 32, 120, 37), _loadoutAreaSelection.Index, _loadoutAreaSelection.GuiContent, _loadoutAreaSelection.Length, BlueStonez.tab_largeicon);

            if (area != _loadoutAreaSelection.Index)
            {
                _loadoutAreaSelection.SetIndex(area);
                SfxManager.Play2dAudioClip(SoundEffectType.UIButtonClick);
            }

            //Draw Loadout Contents
            Rect contentRect = new Rect(1, 72, rect.width - 1, rect.height - 72);
            switch (_loadoutAreaSelection.Current)
            {
                case LoadoutArea.Weapons:
                    DrawWeaponLoadout(contentRect);
                    break;
                case LoadoutArea.Gear:
                    DrawGearLoadout(contentRect);
                    break;
                case LoadoutArea.QuickItems:
                    DrawQuickItemLoadout(contentRect);
                    break;
            }
        }
        GUI.EndGroup();
    }

    private void DrawShop(Rect labsRect)
    {
        _shopArea = labsRect;
        _shopArea.x += _rectLabs.x;
        _shopArea.y += _rectLabs.y;

        bool drawRefreshButton = false;
        if (!Application.isWebPlayer || Application.isEditor)
        {
            drawRefreshButton = true;
        }

        GUI.BeginGroup(labsRect, BlueStonez.window);
        {
            DrawShopTabs(labsRect);

            if (_shopAreaSelection.Current == ShopArea.Inventory ||
                _shopAreaSelection.Current == ShopArea.Shop)
            {
                _searchBar.Draw(new Rect(drawRefreshButton ? labsRect.width - 200 : labsRect.width - 128, 8, 123, 20));
            }

            // Draw the selected subtab selection grid based on the selected main tab.
            switch (_shopAreaSelection.Current)
            {
                case ShopArea.Inventory:
                    DrawItemGUIList(_inventoryItemGUIList, labsRect);
                    DrawSortBar(new Rect(0, 71, labsRect.width, 22), false, true);
                    break;
                case ShopArea.Shop:
                    DrawItemGUIList(_shopItemGUIList, labsRect);
                    DrawShopSubTabs(labsRect, true);
                    break;
                case ShopArea.MysteryBox:
                    _lotteryGui.Draw(new Rect(0, 71, labsRect.width, labsRect.height - 71));
                    break;
                case ShopArea.Packs:
                    _masBundleGui.Draw(new Rect(0, 71, labsRect.width, labsRect.height - 71));
                    break;
                case ShopArea.Credits:
                    _masCreditsGui.Draw(new Rect(0, 71, labsRect.width, labsRect.height - 71));
                    break;
            }
        }

        if (drawRefreshButton)
        {
            bool isEnabled = PlayerDataManager.IsPlayerLoggedIn && GUITools.SaveClickIn(7);
            GUI.enabled = isEnabled || PlayerDataManager.IsPlayerLoggedIn && GUITools.SaveClickIn(7);
            if (GUITools.Button(new Rect(labsRect.width - 66, 7, 64, 20), new GUIContent(LocalizedStrings.Refresh), BlueStonez.buttondark_medium))
            {
                GUITools.Clicked();
                ApplicationDataManager.Instance.RefreshWallet();
            }
            GUI.enabled = isEnabled;
        }

        GUI.EndGroup();
    }

    /// <summary>
    /// Draw the main tabs selection grid.
    /// </summary>
    /// <param name="rect"></param>
    private void DrawShopTabs(Rect rect)
    {
        rect = new Rect(0, 0, rect.width, rect.height);

        GUI.Box(rect, string.Empty, BlueStonez.window);
        GUI.Label(new Rect(1, 0, rect.width, 72), LocalizedStrings.ShopCaps, BlueStonez.tab_strip_large);

        int index = GUI.Toolbar(new Rect(0, 32, _shopAreaSelection.Length * 40, 37), _shopAreaSelection.Index, _shopAreaSelection.GuiContent, BlueStonez.tab_largeicon);
        if (index != _shopAreaSelection.Index)
        {
            _shopAreaSelection.SetIndex(index);
            _searchBar.ClearFilter();
            SfxManager.Play2dAudioClip(SoundEffectType.UIButtonClick);
        }
    }

    /// <summary>
    /// Draw the sub tabs selection grid, based on the selected main tab.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="subTabsContent"></param>
    /// <param name="showLevel"></param>
    /// <param name="showExpDay"></param>
    private void DrawShopSubTabs(Rect position, bool showLevel)
    {
        // Draw the Tabs
        int index = GUI.SelectionGrid(new Rect(4, 71, position.width - 8, 37), _typeSelection.Index, _typeSelection.GuiContent, _typeSelection.Length, BlueStonez.tab_large);
        if (index != _typeSelection.Index)
        {
            _typeSelection.SetIndex(index);
            SfxManager.Play2dAudioClip(SoundEffectType.UIButtonClick);
        }

        // draw filters
        if (_typeSelection.Current == UberstrikeItemType.Weapon)
        {
            DrawWeaponClassFilter(new Rect(4, 103, position.width - 8, 30));
            DrawSortBar(new Rect(0, 137, position.width + 1, 22), showLevel, false);
        }
        else if (_typeSelection.Current == UberstrikeItemType.Gear)
        {
            DrawGearClassFilter(new Rect(4, 103, position.width - 8, 30));
            DrawSortBar(new Rect(0, 137, position.width + 1, 22), showLevel, false);
        }
        else
        {
            DrawSortBar(new Rect(0, 107, position.width, 22), showLevel, false);
        }
    }

    private void DrawWeaponClassFilter(Rect rect)
    {
        GUI.changed = false;
        Rect grid = new Rect(rect.x, rect.y + 5, rect.width, rect.height);
        int index = GUI.SelectionGrid(grid, _weaponClassSelection.Index, _weaponClassSelection.GuiContent, _weaponClassSelection.Length, BlueStonez.tab_large);
        if (GUI.changed)
        {
            if (index == _weaponClassSelection.Index)
            {
                _weaponClassSelection.SetIndex(-1);
            }
            else
            {
                _weaponClassSelection.SetIndex(index);
            }

            SfxManager.Play2dAudioClip(SoundEffectType.UIButtonClick);
        }
    }

    private void DrawGearClassFilter(Rect rect)
    {
        GUI.changed = false;
        Rect grid = new Rect(rect.x, rect.y + 5, rect.width, rect.height);
        int index = GUI.SelectionGrid(grid, _gearClassSelection.Index, _gearClassSelection.GuiContent, _gearClassSelection.Length, BlueStonez.tab_large);
        if (GUI.changed)
        {
            if (index == _gearClassSelection.Index)
            {
                _gearClassSelection.SetIndex(-1);
            }
            else
            {
                _gearClassSelection.SetIndex(index);
            }

            SfxManager.Play2dAudioClip(SoundEffectType.UIButtonClick);
        }
    }

    private void DrawSortBar(Rect sortRect, bool showLevel, bool showExpDay)
    {
        ShopSorting.ItemComparer<BaseItemGUI> comparer = _shopAreaSelection.Current == ShopArea.Shop ? _shopComparer : _inventoryComparer;

        GUI.BeginGroup(sortRect);
        {
            if (!showLevel && showExpDay) // inventory
            {
                if (GUI.Button(new Rect(0, 0, sortRect.width - 134, sortRect.height), string.Empty, BlueStonez.box_grey50)) // name
                {
                    SortShopPages(ShopSortedColumns.Name);
                }
                if (comparer.Column == ShopSortedColumns.Name)
                {
                    if (comparer.Ascending)
                        GUI.Label(new Rect(0, 0, sortRect.width - 134, sortRect.height), new GUIContent(LocalizedStrings.Name, UberstrikeIcons.SortUpArrow), BlueStonez.label_interparkmed_11pt);
                    else
                        GUI.Label(new Rect(0, 0, sortRect.width - 134, sortRect.height), new GUIContent(LocalizedStrings.Name, UberstrikeIcons.SortDownArrow), BlueStonez.label_interparkmed_11pt);
                }
                else
                    GUI.Label(new Rect(0, 0, sortRect.width - 134, sortRect.height), new GUIContent(LocalizedStrings.Name), BlueStonez.label_interparkmed_11pt);

                if (GUI.Button(new Rect(sortRect.width - 181, 0, 113, sortRect.height), string.Empty, BlueStonez.box_grey50)) // duration
                {
                    SortShopPages(ShopSortedColumns.Duration);
                }
                if (comparer.Column == ShopSortedColumns.Duration)
                {
                    if (comparer.Ascending)
                        GUI.Label(new Rect(sortRect.width - 181, 0, 113, sortRect.height), new GUIContent(LocalizedStrings.Duration, UberstrikeIcons.SortUpArrow), BlueStonez.label_interparkmed_11pt);
                    else
                        GUI.Label(new Rect(sortRect.width - 181, 0, 113, sortRect.height), new GUIContent(LocalizedStrings.Duration, UberstrikeIcons.SortDownArrow), BlueStonez.label_interparkmed_11pt);

                }
                else
                    GUI.Label(new Rect(sortRect.width - 136, 0, 64, sortRect.height), new GUIContent(LocalizedStrings.Duration), BlueStonez.label_interparkmed_11pt);

                GUI.Label(new Rect(sortRect.width - 73, 0, 72, sortRect.height), string.Empty, BlueStonez.box_grey50);
            }
            else if (showLevel && !showExpDay) // shop
            {
                //NAME SORTING
                if (GUI.Button(new Rect(0, 0, sortRect.width - 179, sortRect.height), string.Empty, BlueStonez.box_grey50)) // Name
                {
                    SortShopPages(ShopSortedColumns.Name);
                }
                if (comparer.Column == ShopSortedColumns.Name)
                {
                    if (comparer.Ascending)
                        GUI.Label(new Rect(0, 0, sortRect.width - 179, sortRect.height), new GUIContent(LocalizedStrings.Name, UberstrikeIcons.SortUpArrow), BlueStonez.label_interparkmed_11pt);
                    else
                        GUI.Label(new Rect(0, 0, sortRect.width - 179, sortRect.height), new GUIContent(LocalizedStrings.Name, UberstrikeIcons.SortDownArrow), BlueStonez.label_interparkmed_11pt);
                }
                else
                    GUI.Label(new Rect(0, 0, sortRect.width - 179, sortRect.height), new GUIContent(LocalizedStrings.Name), BlueStonez.label_interparkmed_11pt);

                //LEVEL SORTING
                if (GUI.Button(new Rect(sortRect.width - 181, 0, 48, sortRect.height), string.Empty, BlueStonez.box_grey50)) // Level
                {
                    SortShopPages(ShopSortedColumns.Level);
                }
                if (comparer.Column == ShopSortedColumns.Level)
                {
                    if (comparer.Ascending)
                        GUI.Label(new Rect(sortRect.width - 181, 0, 48, sortRect.height), new GUIContent(LocalizedStrings.Level, UberstrikeIcons.SortUpArrow), BlueStonez.label_interparkmed_11pt);
                    else
                        GUI.Label(new Rect(sortRect.width - 181, 0, 48, sortRect.height), new GUIContent(LocalizedStrings.Level, UberstrikeIcons.SortDownArrow), BlueStonez.label_interparkmed_11pt);
                }
                else
                    GUI.Label(new Rect(sortRect.width - 181, 0, 48, sortRect.height), new GUIContent(LocalizedStrings.Level), BlueStonez.label_interparkmed_11pt);

                //PRICE SORTING
                if (GUI.Button(new Rect(sortRect.width - 134, 0, 65, sortRect.height), string.Empty, BlueStonez.box_grey50)) // Price
                {
                    SortShopPages(ShopSortedColumns.PriceShop);
                }
                if (comparer.Column == ShopSortedColumns.PriceShop)
                {
                    if (comparer.Ascending)
                        GUI.Label(new Rect(sortRect.width - 134, 0, 65, sortRect.height), new GUIContent(LocalizedStrings.Price, UberstrikeIcons.SortUpArrow), BlueStonez.label_interparkmed_11pt);
                    else
                        GUI.Label(new Rect(sortRect.width - 134, 0, 65, sortRect.height), new GUIContent(LocalizedStrings.Price, UberstrikeIcons.SortDownArrow), BlueStonez.label_interparkmed_11pt);
                }
                else
                    GUI.Label(new Rect(sortRect.width - 134, 0, 65, sortRect.height), new GUIContent(LocalizedStrings.Price), BlueStonez.label_interparkmed_11pt);

                GUI.Label(new Rect(sortRect.width - 71, 0, 70, sortRect.height), string.Empty, BlueStonez.box_grey50);
            }
        }
        GUI.EndGroup();
    }

    private void DrawItemGUIList<T>(List<T> list, Rect position) where T : BaseItemGUI
    {
        int topOffset = (_typeSelection.Current == UberstrikeItemType.Weapon || _typeSelection.Current == UberstrikeItemType.Gear) ? 58 : 29;

        int selectedItemPosition = -1;
        int selectedItemHeight = 0;
        int height = 60;

        int y = _shopAreaSelection.Current == ShopArea.Inventory ? 93 : (100 + topOffset);

        Rect scrollRect = new Rect(0, y, position.width, position.height - (y + 1));

        Rect contentRect = new Rect(0, 0, position.width - 20, ((list.Count - _skippedDefaultGearCount) * height) + 106);
        bool showTooltip = scrollRect.Contains(Event.current.mousePosition);
        _labScroll = GUI.BeginScrollView(scrollRect, _labScroll, contentRect);
        {
            //decrease the width of the content when there is a scrollbar
            int decreaseWidth = contentRect.height > scrollRect.height ? 20 : 5;

            _skippedDefaultGearCount = 0;

            for (int i = 0; i < list.Count; i++)
            {
                if (!_searchBar.CheckIfPassFilter(list[i].Item.Name))
                {
                    _skippedDefaultGearCount++;
                    continue;
                }
                if (_itemFilter != null && !_itemFilter.CanPass(list[i].Item))
                {
                    _skippedDefaultGearCount++;
                    continue;
                }

                int top = height * (i - _skippedDefaultGearCount);

                Rect itemRect = new Rect(0, top + ((selectedItemPosition == -1) ? 0 : selectedItemHeight - 20), position.width - decreaseWidth, height);
                Rect hoverRect = new Rect(itemRect.x, itemRect.y, itemRect.width - 100, itemRect.height);

                list[i].Draw(itemRect, itemRect.Contains(Event.current.mousePosition));

                if (showTooltip && hoverRect.Contains(Event.current.mousePosition) && !DragAndDrop.Instance.IsDragging)
                {
                    _itemTooltip.SetItem(list[i].Item, itemRect, PopupViewSide.Left);
                }

                //do drag/drop control
                DragAndDrop.Instance.DrawSlot(itemRect, new ShopDragSlot() { Item = list[i].Item, Slot = LoadoutSlotType.Shop }, OnDropShop);
            }
        }
        GUI.EndScrollView();
    }

    private void UpdateItemFilter()
    {
        switch (_shopAreaSelection.Current)
        {
            case ShopArea.Inventory:
                {
                    _itemFilter = new InventoryItemFilter();
                }
                break;

            case ShopArea.Shop:
                switch (_typeSelection.Current)
                {
                    case UberstrikeItemType.Special:
                        {
                            _itemFilter = new SpecialItemFilter();
                        }
                        break;

                    case UberstrikeItemType.QuickUse:
                    case UberstrikeItemType.Functional:
                        {
                            _itemFilter = new ItemByTypeFilter(_typeSelection.Current);
                        }
                        break;

                    case UberstrikeItemType.Gear:
                        if (_gearClassSelection.Current == 0)
                        {
                            _itemFilter = new ItemByTypeFilter(_typeSelection.Current);
                        }
                        else
                        {
                            _itemFilter = new ItemByClassFilter(_typeSelection.Current, _gearClassSelection.Current);
                        }
                        break;
                    case UberstrikeItemType.Weapon:
                        if (_weaponClassSelection.Current == 0)
                        {
                            _itemFilter = new ItemByTypeFilter(_typeSelection.Current);
                        }
                        else
                        {
                            _itemFilter = new ItemByClassFilter(_typeSelection.Current, _weaponClassSelection.Current);
                        }
                        break;
                }
                break;
        }
    }

    #endregion

    #region Loadout GUI

    private void DrawWeaponLoadout(Rect position)
    {
        _loadoutWeaponScroll = GUI.BeginScrollView(position, _loadoutWeaponScroll, new Rect(0, 0, position.width - 20, (SlotHeight * 4) + 5));
        {
            string[] slotNames = new string[]
            {
                LocalizedStrings.Melee, LocalizedStrings.PrimaryWeapon, LocalizedStrings.SecondaryWeapon, LocalizedStrings.TertiaryWeapon
            };
            LoadoutSlotType[] slotTypes = new LoadoutSlotType[]
            {
                LoadoutSlotType.WeaponMelee, LoadoutSlotType.WeaponPrimary, LoadoutSlotType.WeaponSecondary, LoadoutSlotType.WeaponTertiary
            };

            Rect highlightRect = new Rect();

            for (int i = 0; i < 4; i++)
            {
                Rect itemRect = new Rect(0, SlotHeight * i, position.width - 5, SlotHeight);
                DrawLoadoutWeaponItem(slotNames[i], LoadoutManager.Instance.GetItemOnSlot(slotTypes[i]), itemRect, slotTypes[i]);

                if (slotTypes[i] == InventoryManager.Instance.CurrentWeaponSlot)
                {
                    highlightRect.x = itemRect.x + 5;
                    highlightRect.y = itemRect.y;
                    highlightRect.width = itemRect.width - 16;
                    highlightRect.height = itemRect.height - 10;
                }
            }

            GUI.color = new Color(1, 1, 1, 0.5f);
            GUI.Box(highlightRect, GUIContent.none, BlueStonez.group_grey81);
            GUI.color = Color.white;

            if (_showRenewLoadoutButton)
            {
                Rect[] toggleRects = new Rect[]
                {
                    new Rect(0, SlotHeight * 0, 5, SlotHeight),
                    new Rect(0, SlotHeight * 1, 5, SlotHeight ),
                    new Rect(0, SlotHeight * 2, 5, SlotHeight ),
                    new Rect(0, SlotHeight * 3, 5, SlotHeight )
                };

                for (int i = 0; i < LoadoutManager.WeaponSlots.Length; i++)
                {
                    LoadoutSlotType slot = LoadoutManager.WeaponSlots[i];

                    _renewItem[slot] = GUI.Toggle(toggleRects[i], _renewItem[slot], _renewItem[slot] ? ">" : "<", BlueStonez.panelquad_toggle);
                }
            }

            GUI.color = Color.white;
        }
        GUI.EndScrollView();
    }

    private void DrawGearLoadout(Rect position)
    {
        Rect[] gearRects = new Rect[]
        {
            new Rect(0, SlotHeight * 1, position.width - 5, SlotHeight),
            new Rect(0, SlotHeight * 2, position.width - 5, SlotHeight),
            new Rect(0, SlotHeight * 3, position.width - 5, SlotHeight),
            new Rect(0, SlotHeight * 4, position.width - 5, SlotHeight),
            new Rect(0, SlotHeight * 5, position.width - 5, SlotHeight),
            new Rect(0, SlotHeight * 6, position.width - 5, SlotHeight)
        };

        Rect[] toggleRects = new Rect[]
        {
            new Rect(0, 0, 5, SlotHeight - 10),
            new Rect(0, SlotHeight - 10, 5, SlotHeight),
            new Rect(0, SlotHeight * 2 - 10, 5, SlotHeight),
            new Rect(0, SlotHeight * 3 - 10, 5, SlotHeight),
            new Rect(0, SlotHeight * 4 - 10, 5, SlotHeight),
            new Rect(0, SlotHeight * 5 - 10, 5, SlotHeight)
        };

        _loadoutGearScroll = GUI.BeginScrollView(position, _loadoutGearScroll, new Rect(0, 0, position.width - 20, SlotHeight * 7));
        {
            DrawLoadoutGearItem(LocalizedStrings.Holo, LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.GearHolo), LoadoutSlotType.GearHolo, new Rect(0, 0, position.width - 5, SlotHeight), UberstrikeItemClass.GearHolo);

            for (int i = 0; i < LoadoutManager.GearSlots.Length; i++)
            {
                string name = LoadoutManager.GearSlotNames[i];
                LoadoutSlotType slot = LoadoutManager.GearSlots[i];

                //don't draw default items
                DrawLoadoutGearItem(name, LoadoutManager.Instance.GetItemOnSlot(slot), slot, gearRects[i], LoadoutManager.GearSlotClasses[i]);
            }

            if (_showRenewLoadoutButton)
            {
                for (int i = 0; i < LoadoutManager.GearSlots.Length; i++)
                {
                    Rect rect = toggleRects[i];
                    LoadoutSlotType slot = LoadoutManager.GearSlots[i];

                    _renewItem[slot] = GUI.Toggle(rect, _renewItem[slot], _renewItem[slot] ? ">" : "<", BlueStonez.panelquad_toggle);
                }
            }
        }
        GUI.EndScrollView();
    }

    private void DrawQuickItemLoadout(Rect position)
    {
        _loadoutQuickUseFuncScroll = GUI.BeginScrollView(position, _loadoutQuickUseFuncScroll, new Rect(0, 0, position.width - 20, (SlotHeight * 4) + 5));
        {
            Rect highlightRect = new Rect();

            for (int i = 0; i < 3; i++)
            {
                DrawLoadoutQuickUseItem(LocalizedStrings.QuickItem + " " + (i + 1).ToString(),
                LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.QuickUseItem1 + i),
                LoadoutSlotType.QuickUseItem1 + i,
                new Rect(0, SlotHeight * i, position.width - 5, SlotHeight),
                InputManager.Instance.GetKeyAssignmentString(GameInputKey.QuickItem1 + i));

                if (InventoryManager.Instance.CurrentQuickItemSot == (LoadoutSlotType.QuickUseItem1 + i))
                {
                    highlightRect.x = 0 + 5;
                    highlightRect.y = SlotHeight * i;
                    highlightRect.width = position.width - 16;
                    highlightRect.height = SlotHeight - 10;
                }
            }

            GUI.color = new Color(1, 1, 1, 0.5f);
            GUI.Box(highlightRect, GUIContent.none, BlueStonez.group_grey81);
            GUI.color = Color.white;

            GUI.color = Color.white;
        }
        GUI.EndScrollView();
    }


    private void DrawLoadoutWeaponItem(string slotName, InventoryItem item, Rect rect, LoadoutSlotType slot)
    {
        Rect contentRect = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10);

        GUI.BeginGroup(contentRect);
        {
            if (item.Item != null)
            {
                GUI.Label(new Rect(contentRect.width - 60, 0, 48, 48), item.Item.Icon, BlueStonez.item_slot_large);
                GUI.Label(new Rect(0, 5, contentRect.width - 65, 18), slotName, BlueStonez.label_interparkmed_18pt_right);
                GUI.Label(new Rect(0, 30, contentRect.width - 65, 12), item.Item.Name, BlueStonez.label_interparkmed_10pt_right);
                GUI.Label(new Rect(0, contentRect.height - 1, contentRect.width, 1), string.Empty, BlueStonez.horizontal_line_grey95);
            }
            else
            {
                GUI.Label(new Rect(contentRect.width - 60, 0, 48, 48), GUIContent.none, BlueStonez.item_slot_large);
                GUI.Label(new Rect(0, 5, contentRect.width - 65, 18), slotName, BlueStonez.label_interparkmed_18pt_right);
                GUI.Label(new Rect(0, contentRect.height - 1, contentRect.width, 1), string.Empty, BlueStonez.horizontal_line_grey95);
            }
        }
        GUI.EndGroup();

        if (rect.Contains(Event.current.mousePosition))
        {
            if (Event.current.type == EventType.MouseDown)
            {
                if (InventoryManager.Instance.CurrentWeaponSlot != slot)
                {
                    InventoryManager.Instance.CurrentWeaponSlot = slot;
                    GameState.LocalDecorator.SetActiveWeaponSlot(slot);
                    GameState.LocalDecorator.ShowWeapon(slot);

                    InventoryItem i;
                    if (LoadoutManager.Instance.TryGetItemInSlot(slot, out i))
                    {
                        AvatarAnimationManager.Instance.SetAnimationState(PageType.Shop, i.Item.ItemClass);
                    }
                    else
                    {
                        AvatarAnimationManager.Instance.SetAnimationState(PageType.Shop, 0);
                    }
                }
            }
            else
            {
                _itemTooltip.SetItem(item.Item, contentRect, PopupViewSide.Left, item.DaysRemaining);
            }

        }

        //highlight possible WEAPON drop areas in white
        Color? color = null;
        if (DragAndDrop.Instance.IsDragging && DragAndDrop.Instance.DraggedItem.Item.ItemClass == UberstrikeItemClass.WeaponMelee && slot == LoadoutSlotType.WeaponMelee)
        {
            color = new Color(1, 1, 1, 0.1f);
        }
        else if (DragAndDrop.Instance.IsDragging && DragAndDrop.Instance.DraggedItem.Item.ItemClass != UberstrikeItemClass.WeaponMelee && slot != LoadoutSlotType.WeaponMelee)
        {
            color = new Color(1, 1, 1, 0.1f);
        }
        else if (slot == _highlightedSlot)
        {
            color = new Color(1, 1, 1, _highlightedSlotAlpha);
        }

        Rect dragRect = new Rect(contentRect.x, contentRect.y - 5, contentRect.width - 6, contentRect.height);
        DragAndDrop.Instance.DrawSlot(dragRect, new ShopDragSlot() { Item = item.Item, Slot = slot }, OnDropLoadout, color);

        //if (slot == InventoryManager.Instance._currentWeaponSlot)
        //{
        //    // selected weapon frame
        //    GUI.color = new Color(1, 1, 1, 0.5f);
        //    GUI.Box(new Rect(contentRect.x, contentRect.y - 5, contentRect.width - 6, contentRect.height), GUIContent.none, BlueStonez.group_grey81);
        //    GUI.color = Color.white;
        //}
    }

    private void DrawLoadoutGearItem(string slotName, InventoryItem item, LoadoutSlotType loadoutSlotType, Rect rect, UberstrikeItemClass itemClass)
    {
        Rect contentRect = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10);

        GUI.BeginGroup(contentRect);
        {
            if (item.Item != null && !ItemManager.Instance.IsDefaultGearItem(item.Item.ItemId))
            {
                GUI.Label(new Rect(contentRect.width - 60, 0, 48, 48), item.Item.Icon, BlueStonez.item_slot_large);
                GUI.Label(new Rect(0, 5, contentRect.width - 65, 18), slotName, BlueStonez.label_interparkmed_18pt_right);
                GUI.Label(new Rect(0, 30, contentRect.width - 65, 12), item.Item.Name, BlueStonez.label_interparkmed_10pt_right);
            }
            else
            {
                GUI.Label(new Rect(contentRect.width - 60, 0, 48, 48), GUIContent.none, BlueStonez.item_slot_large);
                GUI.Label(new Rect(0, 5, contentRect.width - 65, 18), slotName, BlueStonez.label_interparkmed_18pt_right);
            }

            GUI.Label(new Rect(0, contentRect.height - 5, contentRect.width, 1), string.Empty, BlueStonez.horizontal_line_grey95);
        }
        GUI.EndGroup();

        if (rect.Contains(Event.current.mousePosition))
        {
            if (Event.current.type == EventType.MouseDown)
            {
                if (item.Item != null)
                {
                    AvatarAnimationManager.Instance.SetAnimationState(PageType.Shop, item.Item.ItemClass);
                }
            }
            else
            {
                _itemTooltip.SetItem(item.Item, contentRect, PopupViewSide.Left, item.DaysRemaining);
            }
        }

        //highlight possible GEAR drop areas in white
        Color? color = null;
        if (DragAndDrop.Instance.IsDragging && DragAndDrop.Instance.DraggedItem.Item.ItemClass == itemClass)
        {
            color = new Color(1, 1, 1, 0.2f);
        }
        else if (loadoutSlotType == _highlightedSlot)
        {
            color = new Color(1, 1, 1, _highlightedSlotAlpha);
        }

        DragAndDrop.Instance.DrawSlot(new Rect(contentRect.x, contentRect.y - 15, contentRect.width, contentRect.height + 11), new ShopDragSlot() { Item = item.Item, Slot = loadoutSlotType }, OnDropLoadout, color);
    }

    private void DrawLoadoutQuickUseItem(string slotName, InventoryItem itemQuickUse, LoadoutSlotType loadoutSlotType, Rect rect, string slotTag)
    {
        Rect contentRect = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10);

        GUI.BeginGroup(contentRect);
        {
            if (itemQuickUse != null && itemQuickUse.Item != null)
            {
                var qi = itemQuickUse.Item.ItemView as QuickItemConfiguration;
                if (qi != null)
                {
                    GUI.Label(new Rect(contentRect.width - 60, 0, 48, 48), itemQuickUse.Item.Icon, BlueStonez.item_slot_large);

                    GUI.Label(new Rect(3, 5, contentRect.width - 65, 26), itemQuickUse.Item.Name, BlueStonez.label_interparkbold_13pt_left);
                    if (itemQuickUse.AmountRemaining > 0)
                    {
                        GUI.Label(new Rect(3, 32, contentRect.width - 65, 12),
                           string.Format("Amount: {0} / {1}", qi.UsesPerGame, itemQuickUse.AmountRemaining),
                            BlueStonez.label_interparkmed_10pt_left);
                    }
                    GUI.Label(new Rect(0, contentRect.height - 1, contentRect.width, 1), string.Empty, BlueStonez.horizontal_line_grey95);
                }
            }
            else
            {
                GUI.Label(new Rect(contentRect.width - 60, 0, 48, 48), GUIContent.none, BlueStonez.item_slot_large);
                GUI.Label(new Rect(0, 5, contentRect.width - 65, 18), slotName, BlueStonez.label_interparkmed_18pt_right);
                GUI.Label(new Rect(0, contentRect.height - 1, contentRect.width, 1), string.Empty, BlueStonez.horizontal_line_grey95);
            }
        }
        GUI.EndGroup();

        if (rect.Contains(Event.current.mousePosition))
        {
            if (Event.current.type == EventType.MouseDown)
                InventoryManager.Instance.CurrentQuickItemSot = loadoutSlotType;
            else
                _itemTooltip.SetItem(itemQuickUse.Item, contentRect, PopupViewSide.Left);
        }

        //highlight possible QUICK drop areas in white
        Color? color = null;
        if (DragAndDrop.Instance.IsDragging && DragAndDrop.Instance.DraggedItem.Item.ItemType == UberstrikeItemType.QuickUse)
        {
            color = new Color(1, 1, 1, 0.1f);
        }
        else if (loadoutSlotType == _highlightedSlot)
        {
            color = new Color(1, 1, 1, _highlightedSlotAlpha);
        }
        DragAndDrop.Instance.DrawSlot(new Rect(contentRect.x, contentRect.y - 5, contentRect.width - 6, contentRect.height), new ShopDragSlot() { Item = itemQuickUse.Item, Slot = loadoutSlotType }, OnDropLoadout, color);
    }

    # endregion

    #region Drag Zones

    private void OnDropAvatar(int slotId, ShopDragSlot item)
    {
        if (item.Slot == LoadoutSlotType.Shop)
        {
            EquipItemFromArea(item.Item, LoadoutSlotType.None, ShopArea.Shop);
        }
        else
        {
            UnequipItem(item.Item);
        }
    }

    private void OnDropShop(int slotId, ShopDragSlot item)
    {
        //Debug.Log("OnDropShop");
        if (item.Slot != LoadoutSlotType.Shop)
        {
            UnequipItem(item.Item);
        }
    }

    private void OnDropLoadout(int slotId, ShopDragSlot item)
    {
        InventoryManager.Instance.CurrentWeaponSlot = (LoadoutSlotType)slotId;

        if (item.Slot == LoadoutSlotType.Shop)
        {
            EquipItemFromArea(item.Item, (LoadoutSlotType)slotId, ShopArea.Shop);
        }
        else
        {
            switch ((LoadoutSlotType)slotId)
            {
                // switching weapons
                case LoadoutSlotType.WeaponPrimary:
                case LoadoutSlotType.WeaponSecondary:
                case LoadoutSlotType.WeaponTertiary:
                    if (item.Slot >= LoadoutSlotType.WeaponPrimary &&
                        item.Slot <= LoadoutSlotType.WeaponTertiary)
                    {
                        SwapWeapons(item.Slot, (LoadoutSlotType)slotId);
                    }
                    break;

                //switching quickitems
                case LoadoutSlotType.QuickUseItem1:
                case LoadoutSlotType.QuickUseItem2:
                case LoadoutSlotType.QuickUseItem3:
                    SwapQuickItems(item.Slot, (LoadoutSlotType)slotId);
                    break;
            }
        }
    }

    private void OnBeginDrag(IDragSlot item)
    {
        // automaticly change player loadout tab based on whet you draging
        if (item != null)
        {
            switch (item.Item.ItemType)
            {
                case UberstrikeItemType.Weapon:
                case UberstrikeItemType.WeaponMod:
                    _loadoutAreaSelection.SetIndex(0);
                    SelectLoadoutArea(LoadoutArea.Weapons);
                    break;
                case UberstrikeItemType.Gear:
                    _loadoutAreaSelection.SetIndex(1);
                    SelectLoadoutArea(LoadoutArea.Gear);
                    break;
                case UberstrikeItemType.QuickUse:
                case UberstrikeItemType.Functional:
                    _loadoutAreaSelection.SetIndex(2);
                    SelectLoadoutArea(LoadoutArea.QuickItems);
                    break;
            }
        }
    }

    private void SwapQuickItems(LoadoutSlotType slot, LoadoutSlotType newslot)
    {
        if (LoadoutManager.Instance.SwapLoadoutItems(slot, newslot))
        {
            InventoryManager.Instance.CurrentQuickItemSot = newslot;
            HighlightingSlot(newslot);
        }

        //for (int i = 0; i < _activeLoadoutUsedSpace.Count && i < 3; i++)
        //{
        //    if (_activeLoadoutUsedSpace[i].Contains(Event.current.mousePosition))
        //    {
        //        LoadoutSlotType newslot = i == 0 ? LoadoutSlotType.QuickUseItem1 : i == 1 ? LoadoutSlotType.QuickUseItem2 : LoadoutSlotType.QuickUseItem3;

        //        if (LoadoutManager.Instance.SwapLoadoutItems(slot, newslot))
        //        {
        //            InventoryManager.Instance._currentQuickItemSot = newslot;
        //            //_quickItemLoadoutPickUpSlot = -1;
        //            HighlightingSlot(newslot);
        //        }
        //        break;
        //    }
        //}
    }

    private void SwapWeapons(LoadoutSlotType slot, LoadoutSlotType newslot)
    {
        if (LoadoutManager.Instance.SwapLoadoutItems(slot, newslot))
        {
            InventoryManager.Instance.CurrentWeaponSlot = newslot;
            HighlightingSlot(newslot);
        }
    }

    #endregion

    #region Sorting

    private void SortShopPages(ShopSortedColumns sortedColumn)
    {
        ShopSorting.ItemComparer<BaseItemGUI> compare = null;

        switch (sortedColumn)
        {
            case ShopSortedColumns.PriceShop:
                compare = new ShopSorting.PriceComparer();
                break;
            case ShopSortedColumns.Level:
                compare = new ShopSorting.LevelComparer();
                break;
            case ShopSortedColumns.Duration:
                compare = new ShopSorting.DurationComparer();
                break;
            case ShopSortedColumns.Name:
                compare = new ShopSorting.NameComparer();
                break;
        }

        SortShopBy(compare);
    }

    private void SortShopBy(ShopSorting.ItemComparer<BaseItemGUI> comparer)
    {
        switch (_shopAreaSelection.Current)
        {
            case ShopArea.Inventory:
                {
                    if (_inventoryComparer.GetType() == comparer.GetType())
                    {
                        if (comparer.Column == _inventoryComparer.Column) _inventoryComparer.SwitchOrder();
                    }
                    else
                    {
                        _inventoryComparer = comparer;
                    }
                    break;
                }
            case ShopArea.Shop:
                {
                    if (_shopComparer.GetType() == comparer.GetType())
                    {
                        if (comparer.Column == _shopComparer.Column) _shopComparer.SwitchOrder();
                    }
                    else
                    {
                        _shopComparer = comparer;
                    }
                    break;
                }
        }

        ApplyCurrentSorting();
    }

    private void ApplyCurrentSorting()
    {
        switch (_shopAreaSelection.Current)
        {
            case ShopArea.Inventory:
                {
                    _inventoryItemGUIList.Sort(_inventoryComparer);
                    break;
                }
            case ShopArea.Shop:
                {
                    _shopItemGUIList.Sort(_shopComparer);
                    break;
                }
        }
    }

    #endregion

    public struct ShopDragSlot : IDragSlot
    {
        public int Id { get { return (int)Slot; } }
        public IUnityItem Item { get; set; }
        public LoadoutSlotType Slot { get; set; }
    }
}