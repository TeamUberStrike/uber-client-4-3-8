using System.Numerics;
using UberStrike.Shared;

namespace UberStrike.Server;

public sealed class WeaponRuntime
{
    public int    WeaponId;
    public int    Ammo;
    public int    Reserve;
    public double NextFireTime; // server time the weapon may next fire
    public bool   Reloading;
}

/// <summary>
/// Authoritative server-side player. Nothing in here is ever written directly from a client
/// packet — only via validated systems (movement sim, combat, economy).
/// </summary>
public sealed class PlayerState
{
    public int    EntityId;
    public string SessionToken = "";

    public uint LastProcessedInput; // for client reconciliation (ack)
    public uint LastSeenInputSeq;   // replay/ordering guard

    public MoveState Move;          // position/velocity/grounded/aim (shared type)

    public float Health = 100f;
    public float Armor  = 0f;
    public int   TeamId;

    public WeaponRuntime[] Weapons = Array.Empty<WeaponRuntime>();
    public int    ActiveSlot;
    public double SwitchReadyTime; // server time the active weapon may first fire after a switch

    public long  Currency;
    public int   Kills, Deaths;

    public double SmoothedRtt; // seconds; feeds lag-comp rewind

    public readonly AnomalyTracker  Anomaly = new();
    public readonly HitboxHistory   History = new();
    public readonly HashSet<string> SeenPurchaseKeys = new();

    public WeaponRuntime ActiveWeapon => Weapons[ActiveSlot];
    public bool Alive => Health > 0f;
}
