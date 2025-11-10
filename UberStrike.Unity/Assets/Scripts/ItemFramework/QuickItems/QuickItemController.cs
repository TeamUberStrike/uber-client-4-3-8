using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class QuickItemController : Singleton<QuickItemController>
{
    private BaseQuickItem[] _quickItems;
    private const float CooldownTime = 0.5f;
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

                //configure quick item
                if (_quickItems[i] != null)
                {
                    // item is a consumable when recharge time <= 0
                    if (_quickItems[i].Configuration.RechargeTime <= 0)
                    {
                        int index = i;
                        _quickItems[i].Behaviour.OnActivated += () =>
                        {
                            UseConsumableItem(inventoryItem);
                            Restriction.DecreaseUse(index);
                            NextCooldownFinishTime = Time.time + CooldownTime;
                        };

                        Restriction.InitializeSlot(i, _quickItems[i], inventoryItem.AmountRemaining);
                    }
                    else
                    {
                        _quickItems[i].Behaviour.CurrentAmount = _quickItems[i].Configuration.AmountRemaining;
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
        Restriction = new QuickItemRestriction();

        QuickItemEventListener.Instance.Initialize();
        CmuneEventHandler.AddListener<OnSetPlayerTeamEvent>((ev) => UpdateHudSlot(ev.TeamId));
        CmuneEventHandler.AddListener<InputChangeEvent>(OnInputChanged);
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
        }
    }

    #endregion
}