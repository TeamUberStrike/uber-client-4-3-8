
using UnityEngine;
public class QuickItemRestriction
{
    private RestrictedUsage[] _quickItemRestrictions;

    public bool IsEnabled { get; set; }

    public QuickItemRestriction()
    {
        _quickItemRestrictions = new RestrictedUsage[LoadoutManager.QuickSlots.Length];
        for (int i = 0; i < _quickItemRestrictions.Length; i++)
        {
            _quickItemRestrictions[i] = new RestrictedUsage();
        }
    }

    public void InitializeSlot(int index, BaseQuickItem quickItem = null, int amountRemaining = 0)
    {
        if (index >= 0 && index < _quickItemRestrictions.Length)
        {
            if (IsEnabled && quickItem != null)
            {
                if (quickItem.Configuration.ID != _quickItemRestrictions[index].ItemId)
                    _quickItemRestrictions[index].Init(quickItem);
            }
            else
            {
                _quickItemRestrictions[index].Init();
            }

            _quickItemRestrictions[index].RenewLifeUses();

            if (quickItem != null)
            {
                int currentAmount = Mathf.Min(amountRemaining, _quickItemRestrictions[index].RemainingLifeUses);
                quickItem.Behaviour.CurrentAmount = currentAmount;
                quickItem.Configuration.AmountRemaining = currentAmount;
            }
        }
    }

    public void RenewGameUses()
    {
        foreach (var restrictedUsage in _quickItemRestrictions)
        {
            restrictedUsage.RenewGameUses();
        }
    }

    public void RenewRoundUses()
    {
        foreach (var restrictedUsage in _quickItemRestrictions)
        {
            restrictedUsage.RenewRoundUses();
        }
    }

    public void RenewLifeUses()
    {
        foreach (var restrictedUsage in _quickItemRestrictions)
        {
            restrictedUsage.RenewLifeUses();
        }
    }

    public void DecreaseUse(int index)
    {
        if (IsEnabled)
        {
            if (index < _quickItemRestrictions.Length && index >= 0)
            {
                _quickItemRestrictions[index].DecreaseUse();
            }
        }
    }

    public int GetCurrentAvailableAmount(int index, int inventoryRemainingAmount)
    {
        if (IsEnabled)
        {
            if (index < _quickItemRestrictions.Length && index >= 0)
            {
                var restrictedUsage = _quickItemRestrictions[index];
                if (inventoryRemainingAmount >= restrictedUsage.RemainingLifeUses)
                {
                    return restrictedUsage.RemainingLifeUses;
                }
                else
                {
                    return inventoryRemainingAmount;
                }
            }
        }
        return inventoryRemainingAmount;
    }

    private class RestrictedUsage
    {
        public int RemainingGameUses { get; private set; }
        public int RemainingRoundUses { get; private set; }
        public int RemainingLifeUses { get; private set; }
        public int ItemId { get; private set; }

        private int _totalUsesPerGame;
        private int _totalUsesPerRound;
        private int _totalUsesPerLife;

        public void Init(BaseQuickItem item = null)
        {
            if (item != null)
            {
                ItemId = item.Configuration.ID;
                _totalUsesPerGame = item.Configuration.UsesPerGame > 0 ? item.Configuration.UsesPerGame : int.MaxValue;
                _totalUsesPerRound = item.Configuration.UsesPerRound > 0 ? item.Configuration.UsesPerRound : int.MaxValue;
                _totalUsesPerLife = item.Configuration.UsesPerLife > 0 ? item.Configuration.UsesPerLife : int.MaxValue;
            }
            else
            {
                ItemId = 0;
                _totalUsesPerGame = int.MaxValue;
                _totalUsesPerRound = int.MaxValue;
                _totalUsesPerLife = int.MaxValue;
            }
            RenewGameUses();
        }

        public void RenewGameUses()
        {
            RemainingGameUses = _totalUsesPerGame;
            RemainingRoundUses = _totalUsesPerRound;
            RemainingLifeUses = _totalUsesPerLife;
        }

        public void RenewRoundUses()
        {
            RemainingRoundUses = Mathf.Min(_totalUsesPerRound, RemainingGameUses);
            RenewLifeUses();
        }

        public void RenewLifeUses()
        {
            RemainingLifeUses = Mathf.Min(_totalUsesPerLife, RemainingRoundUses);
        }

        public void DecreaseUse()
        {
            RemainingGameUses = Mathf.Max(RemainingGameUses - 1, 0);
            RemainingRoundUses = Mathf.Max(RemainingRoundUses - 1, 0);
            RemainingLifeUses = Mathf.Max(RemainingLifeUses - 1, 0);
        }
    }
}