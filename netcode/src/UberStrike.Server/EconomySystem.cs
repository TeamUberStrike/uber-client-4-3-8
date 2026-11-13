using UberStrike.Shared;

namespace UberStrike.Server;

/// <summary>
/// Currency/XP mutate ONLY here, through validated events. Purchases are balance-checked
/// and idempotent (no double-spend on retried requests).
/// </summary>
public sealed class EconomySystem
{
    public enum Result { Ok, AlreadyApplied, UnknownItem, Insufficient }

    public Result TryPurchase(PlayerState s, int itemId, string idempotencyKey)
    {
        if (s.SeenPurchaseKeys.Contains(idempotencyKey)) return Result.AlreadyApplied;
        if (!ShopTable.TryGetPrice(itemId, out long price)) return Result.UnknownItem;
        if (s.Currency < price) return Result.Insufficient;

        s.Currency -= price;                 // atomic on authoritative state
        s.SeenPurchaseKeys.Add(idempotencyKey);
        // GrantUnlock(s, itemId);           // TODO: persist unlock
        return Result.Ok;
    }
}
