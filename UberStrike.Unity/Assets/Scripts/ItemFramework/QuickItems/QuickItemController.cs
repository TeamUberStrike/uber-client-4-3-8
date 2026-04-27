using Cmune.Util;
using UberStrike.Core.Types;
using UberStrike.DataCenter.Common.Entities;
using UberStrike.Realtime.Common;
using UnityEngine;

public class QuickItemController : Singleton<QuickItemController>
{
    private BaseQuickItem[] _quickItems;
    private InventoryItem[] _inventoryItems;
    private const float CooldownTime = 0.5f;
    private const float SpringGrenadeCooldown = 2f;
    private const int SpringGrenadeCooldownMs = 2000;
    private bool _isEnabled;

    public bool IsEnabled
    {
        get { return _isEnabled && !GameState.CurrentGame.IsWaitingForPlayers; }
        set 
        {
            _isEnabled = value; 
        }
    }

    public bool IsCharging { get; set; }
    public bool IsConsumptionEnabled { get; set; }
    public int CurrentSlotIndex { get; private set; }
    public float NextCooldownFinishTime { get; set; }
    public QuickItemRestriction Restriction { get; private set; }

    public void Initialize()
    {
        Clear();

        for (int i = 0; i < LoadoutManager.QuickSlots.Length; i++)
        {
            LoadoutSlotType slot = LoadoutManager.QuickSlots[i];

            InventoryItem inventoryItem;
            if (LoadoutManager.Instance.TryGetItemInSlot(slot, out inventoryItem) && inventoryItem.Item is QuickItem)
            {
                QuickItem quickItem = inventoryItem.Item as QuickItem;
                _quickItems[i] = quickItem.Instantiate();
                _quickItems[i].transform.parent = GameState.LocalPlayer.WeaponAttachPoint;
                _inventoryItems[i] = inventoryItem;

                //configure quick item
                if (_quickItems[i] != null)
                {
                    // Config overrides are now applied at QuickItem construction time
                    // so shop tooltips see them too — nothing to do here.

                    // item is a consumable when recharge time <= 0
                    if (_quickItems[i].Configuration.RechargeTime <= 0)
                    {
                        int index = i;
                        var capturedItem = inventoryItem;
                        var capturedBehaviour = _quickItems[index];
                        _quickItems[i].Behaviour.OnActivated += () =>
                        {
                            UseConsumableItem(capturedItem);
                            Restriction.DecreaseUse(index);
                            NextCooldownFinishTime = Time.time + GetCooldownFor(capturedBehaviour);
                        };

                        int stock = ResolveStock(_quickItems[i], inventoryItem);
                        Restriction.InitializeSlot(i, _quickItems[i], stock);
                    }
                    else
                    {
                        // Rechargeable items (HealthBot / AmmoBot Charged). Wire the same
                        // Restriction pipeline so per-life caps work; without this the
                        // "charged" variants would be unlimited while alive.
                        int index = i;
                        _quickItems[i].Behaviour.OnActivated += () =>
                        {
                            Restriction.DecreaseUse(index);
                            NextCooldownFinishTime = Time.time + CooldownTime;
                        };

                        int stock = ResolveStock(_quickItems[i], inventoryItem);
                        Restriction.InitializeSlot(i, _quickItems[i], stock);
                        _quickItems[i].Behaviour.CurrentAmount = stock;
                    }
                    _quickItems[i].Behaviour.FocusKey = GetFocusKey(slot);

                    WeaponsHud.Instance.SetQuickItemCurrentAmount(i, _quickItems[i].Behaviour.CurrentAmount);
                    WeaponsHud.Instance.SetQuickItemCooldownMax(i, _quickItems[i].Behaviour.CoolDownTimeTotal);
                    WeaponsHud.Instance.SetQuickItemRechargingMax(i, _quickItems[i].Behaviour.ChargingTimeTotal);
                }

                //hook into the OnProjectileEmitted event and do a network call
                var projectile = _quickItems[i] as IGrenadeProjectile;
                if (projectile != null)
                {
                    projectile.OnProjectileEmitted += (p) =>
                    {
                        ProjectileManager.Instance.AddProjectile(p, WeaponController.Instance.NextProjectileId());

                        GameState.CurrentGame.EmitQuickItem(p.Position, p.Velocity, inventoryItem.Item.ItemId, GameState.LocalCharacter.PlayerNumber, p.ID);
                    };
                }
            }
            else
            {
                Restriction.InitializeSlot(i);
            }
        }

        UpdateHudSlot(GameState.LocalCharacter.TeamID);
        ResetSlotSelection();
        WeaponsHud.Instance.QuickItems.Collapse();
    }

    public void ResetSlotSelection()
    {
        if (_quickItems.Length > 0)
        {
            CurrentSlotIndex = 0;
            if (!IsSlotAvailable(CurrentSlotIndex))
            {
                CurrentSlotIndex = GetNextAvailableSlotIndex(CurrentSlotIndex);
            }
        }

        WeaponsHud.Instance.QuickItems.SetSelected(CurrentSlotIndex);
    }

    public void UpdateQuickSlotAmount()
    {
        for (int i = 0; i < _quickItems.Length; i++)
        {
            if (_quickItems[i] != null)
            {
                WeaponsHud.Instance.SetQuickItemCurrentAmount(i, _quickItems[i].Behaviour.CurrentAmount);
            }
        }
    }

    public void UseQuickItem(LoadoutSlotType slot)
    {
        UseQuickItem(GetSlotIndex(slot));
    }

    private void UseQuickItem(int index)
    {
        if (!IsEnabled || IsCharging || Time.time < NextCooldownFinishTime)
        {
            return;
        }

        if (_quickItems != null && index >= 0 && _quickItems[index] != null)
        {
            if (_quickItems[index].Behaviour.Run())
            {
                if (GameState.LocalPlayer.Character != null)
                    SfxManager.Play2dAudioClip(SoundEffectType.WeaponWeaponSwitch);
            }
        }
        else
        {
            Debug.LogError("The QuickItem has no Behaviour: " + index);
        }
    }

    public void Update()
    {
        if (_quickItems != null)
        {
            for (int i = 0; i < _quickItems.Length; i++)
            {
                if (_quickItems[i] != null)
                {
                    WeaponsHud.Instance.SetQuickItemCooldown(i, _quickItems[i].Behaviour.CoolDownTimeRemaining);
                    WeaponsHud.Instance.SetQuickItemRecharging(i, _quickItems[i].Behaviour.ChargingTimeRemaining);
                }
            }
        }
    }

    #region Private

    private QuickItemController()
    {
        _quickItems = new BaseQuickItem[LoadoutManager.QuickSlots.Length];
        _inventoryItems = new InventoryItem[LoadoutManager.QuickSlots.Length];
        Restriction = new QuickItemRestriction();

        QuickItemEventListener.Instance.Initialize();
        CmuneEventHandler.AddListener<OnSetPlayerTeamEvent>((ev) => UpdateHudSlot(ev.TeamId));
        CmuneEventHandler.AddListener<InputChangeEvent>(OnInputChanged);
        // Per-life-capped items (HealthBot/AmmoBot Charged at UsesPerLife=1) need
        // their counter reset on respawn. Restriction.RenewLifeUses hooks the data
        // layer; syncing Behaviour.CurrentAmount brings the HUD back in sync.
        CmuneEventHandler.AddListener<OnPlayerRespawnEvent>(OnLocalPlayerRespawn);
    }

    private void OnLocalPlayerRespawn(OnPlayerRespawnEvent ev)
    {
        Restriction.RenewLifeUses();
        for (int i = 0; i < _quickItems.Length; i++)
        {
            var qi = _quickItems[i];
            var inv = _inventoryItems[i];
            if (qi == null || inv == null) continue;
            int stock = ResolveStock(qi, inv);
            qi.Behaviour.CurrentAmount = stock;
            WeaponsHud.Instance.SetQuickItemCurrentAmount(i, stock);
        }
    }

    internal static void ApplyItemRuleOverrides(QuickItemConfiguration cfg)
    {
        if (cfg == null) return;
        switch (cfg.BehaviourType)
        {
            case QuickItemLogic.SpringGrenade:
                SetUsesPerLife(cfg, 99);
                SetCoolDownTime(cfg, SpringGrenadeCooldownMs);
                var sg = cfg as SpringGrenadeConfiguration;
                if (sg != null) sg.LifeTime = 15;
                break;
            case QuickItemLogic.ExplosiveGrenade:
                SetUsesPerLife(cfg, 99);
                cfg.AmountRemaining = 100;
                break;
            case QuickItemLogic.HealthPack:
            case QuickItemLogic.AmmoPack:
                // Description: 1 use per life — applied to both basic + charged variants.
                SetUsesPerLife(cfg, 1);
                cfg.AmountRemaining = 1;
                break;
        }
    }

    // UsesPerLife / CoolDownTime live on the external UberStrikeItemQuickView DTO
    // (UberStrike.UnitySdk.dll). Whether they expose public setters is implementation-
    // dependent, so use reflection: try the property setter first, fall back to the
    // private backing field (compiler-generated autoprop name or common _fieldName).
    private static void SetUsesPerLife(QuickItemConfiguration cfg, int value)
    {
        SetBackingValue(cfg, "UsesPerLife", "_usesPerLife", value);
    }

    private static void SetCoolDownTime(QuickItemConfiguration cfg, int valueMs)
    {
        SetBackingValue(cfg, "CoolDownTime", "_coolDownTime", valueMs);
    }

    private static void SetBackingValue(object target, string propName, string fieldName, object value)
    {
        if (target == null) return;
        var type = target.GetType();
        while (type != null)
        {
            var prop = type.GetProperty(propName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(target, value, null);
                return;
            }
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?? type.GetField("<" + propName + ">k__BackingField",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }
            type = type.BaseType;
        }
        Debug.LogWarning("[QuickItemController] Couldn't set " + propName + " on " + target.GetType().Name);
    }

    private static int ResolveStock(BaseQuickItem item, InventoryItem inv)
    {
        var cfg = item.Configuration;
        switch (cfg.BehaviourType)
        {
            case QuickItemLogic.SpringGrenade:
                // Shop sells Spring Grenades in packs: "Spring Grenades: 1" / ": 3" / ": 8".
                // Pack size = last integer in the item name. Dev server returns
                // AmountRemaining=0 for equipped items so we can't trust it.
                return ParsePackSize(inv.Item.Name, defaultStock: 1);
            case QuickItemLogic.ExplosiveGrenade:
                return 100;
            case QuickItemLogic.HealthPack:
            case QuickItemLogic.AmmoPack:
                return 1;
            default:
                return inv.AmountRemaining > 0 ? inv.AmountRemaining : 99;
        }
    }

    private static int ParsePackSize(string itemName, int defaultStock)
    {
        if (string.IsNullOrEmpty(itemName)) return defaultStock;
        var m = System.Text.RegularExpressions.Regex.Match(itemName, @"(\d+)\s*$");
        int n;
        if (m.Success && int.TryParse(m.Groups[1].Value, out n) && n > 0) return n;
        return defaultStock;
    }

    private static float GetCooldownFor(BaseQuickItem item)
    {
        return item.Configuration.BehaviourType == QuickItemLogic.SpringGrenade
            ? SpringGrenadeCooldown
            : CooldownTime;
    }

    private void OnInputChanged(InputChangeEvent ev)
    {
        if (ev.IsDown && !LevelCamera.Instance.IsZoomedIn && IsEnabled)
        {
            switch (ev.Key)
            {
                case GameInputKey.QuickItem1:
                    UseQuickItem(LoadoutSlotType.QuickUseItem1);
                    break;
                case GameInputKey.QuickItem2:
                    UseQuickItem(LoadoutSlotType.QuickUseItem2);
                    break;
                case GameInputKey.QuickItem3:
                    UseQuickItem(LoadoutSlotType.QuickUseItem3);
                    break;
                case GameInputKey.NextQuickItem:
                    if (_quickItems.Length > 0)
                    {
                        CurrentSlotIndex = GetNextAvailableSlotIndex(CurrentSlotIndex);
                        WeaponsHud.Instance.QuickItems.SetSelected(CurrentSlotIndex);
                    }
                    break;
                case GameInputKey.UseQuickItem:
                    UseQuickItem(CurrentSlotIndex);
                    break;
            }
        }
    }

    private int GetNextAvailableSlotIndex(int currentSlot)
    {
        int slot = (currentSlot + 1) % _quickItems.Length;
        while (slot != currentSlot)
        {
            if (!WeaponsHud.Instance.QuickItems.GetLoadoutQuickItemHud(slot).IsEmpty)
            {
                return slot;
            }
            slot = (slot + 1) % _quickItems.Length;
        }
        return currentSlot;
    }

    private void UpdateHudSlot(TeamID teamId)
    {
        for (int i = 0; i < _quickItems.Length; i++)
        {
            var item = _quickItems[i];
            WeaponsHud.Instance.QuickItems.ConfigureQuickItem(i, item ? item.Configuration : null, teamId);
        }

        WeaponsHud.Instance.QuickItems.SetSelected(CurrentSlotIndex);
    }

    private bool IsSlotAvailable(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < _quickItems.Length)
        {
            var item = _quickItems[slotIndex];
            return item != null;
        }
        return false;
    }

    private void UseConsumableItem(InventoryItem inventoryItem)
    {
        if (IsConsumptionEnabled)
        {
            UberStrike.WebService.Unity.ShopWebServiceClient.UseConsumableItem(PlayerDataManager.CmidSecure,
                inventoryItem.Item.ItemId, null, null);
            inventoryItem.AmountRemaining = inventoryItem.AmountRemaining - 1;
            if (inventoryItem.AmountRemaining == 0)
            {
                //refresh inventory to delete the consumable item
                MonoRoutine.Start(ItemManager.Instance.StartGetInventory(false));
            }
        }
    }

    private LoadoutSlotType GetSlotType(int index)
    {
        return LoadoutSlotType.QuickUseItem1 + index;
    }

    private GameInputKey GetFocusKey(LoadoutSlotType slot)
    {
        switch (slot)
        {
            case LoadoutSlotType.QuickUseItem1: return GameInputKey.QuickItem1;
            case LoadoutSlotType.QuickUseItem2: return GameInputKey.QuickItem2;
            case LoadoutSlotType.QuickUseItem3: return GameInputKey.QuickItem3;
            default: return GameInputKey.None;
        }
    }

    private int GetSlotIndex(LoadoutSlotType slot)
    {
        switch (slot)
        {
            case LoadoutSlotType.QuickUseItem1: return 0;
            case LoadoutSlotType.QuickUseItem2: return 1;
            case LoadoutSlotType.QuickUseItem3: return 2;
            default: return -1;
        }
    }

    internal void Reset()
    {
        //e.g. reset the amount of the old spring grenades
        //throw new System.NotImplementedException();
    }

    internal void Clear()
    {
        for (int i = 0; i < _quickItems.Length; i++)
        {
            if (_quickItems[i] != null)
            {
                GameObject.Destroy(_quickItems[i].gameObject);
                _quickItems[i] = null;
            }
            _inventoryItems[i] = null;
        }
    }

    #endregion
}