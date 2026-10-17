using UberStrike.Shared;

namespace UberStrike.Client;

/// <summary>
/// Client combat: predicts COSMETIC feedback immediately (muzzle flash, optimistic ammo)
/// but treats every OUTCOME (hits, damage, kills) as authoritative — applied only from the
/// server's HitEvent. Never predict damage you can't compute.
/// </summary>
public sealed class CombatClient
{
    public int EntityId { get; }
    private readonly string _token;
    private readonly int    _activeSlot;
    private readonly double _fireInterval;
    private int    _predictedAmmo;
    private double _nextFire;

    public CombatClient(int entityId, string sessionToken, int activeSlot, int magSize, double fireInterval)
    {
        EntityId = entityId; _token = sessionToken; _activeSlot = activeSlot;
        _predictedAmmo = magSize; _fireInterval = fireInterval;
    }

    public int PredictedAmmo => _predictedAmmo;

    public event Action? MuzzleFlash;
    public event Action<HitEvent>? ConfirmedHit; // you hit someone (server-confirmed)
    public event Action<float>? DamageTaken;     // server says you took damage

    /// <summary>Mirror of the server's fire gate, for responsive UX. Returns intent to send, or null.</summary>
    public FireIntent? TryFire(double now, uint clientTick)
    {
        if (_predictedAmmo <= 0 || now < _nextFire) return null;
        _predictedAmmo--;                 // optimistic; reconciled by server
        _nextFire = now + _fireInterval;
        MuzzleFlash?.Invoke();            // cosmetic only
        return new FireIntent { EntityId = EntityId, SessionToken = _token, Slot = _activeSlot, ClientTick = clientTick };
    }

    public void OnHitEvent(in HitEvent e)
    {
        if (e.Shooter == EntityId) ConfirmedHit?.Invoke(e);
        if (e.Target  == EntityId) DamageTaken?.Invoke(e.Damage);
    }

    /// <summary>Server truth always wins over the optimistic prediction.</summary>
    public void ReconcileAmmo(int authoritativeAmmo) => _predictedAmmo = authoritativeAmmo;
}
