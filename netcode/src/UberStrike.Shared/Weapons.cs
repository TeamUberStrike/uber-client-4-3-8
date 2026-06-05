namespace UberStrike.Shared;

/// <summary>Server-side weapon definition. Damage/firerate/spread live here, never on the client.</summary>
public sealed class WeaponDef
{
    public int    Id;
    public string Name = "";
    public float  BaseDamage;
    public float  FireInterval;   // seconds between shots (firerate ceiling)
    public int    MagSize;
    public float  Range;
    public float  HeadshotMult = 2f;
    public float  MaxSpreadDeg;   // applied server-side
    public float  FalloffStart;   // distance where damage begins to drop
    public float  FalloffEnd;     // distance where damage hits the floor
    public float  MinFalloffMult = 0.4f;

    public float RangeFalloff(float dist)
    {
        if (dist <= FalloffStart) return 1f;
        if (dist >= FalloffEnd)   return MinFalloffMult;
        float k = (dist - FalloffStart) / MathF.Max(0.001f, FalloffEnd - FalloffStart);
        return 1f - k * (1f - MinFalloffMult);
    }
}

/// <summary>Authoritative weapon catalog. The client only ever sends a slot index.</summary>
public static class WeaponTable
{
    private static readonly Dictionary<int, WeaponDef> Defs = new()
    {
        [1] = new WeaponDef { Id = 1, Name = "MachineGun",  BaseDamage = 18f, FireInterval = 0.10f, MagSize = 40, Range = 120f, MaxSpreadDeg = 2.0f, FalloffStart = 30f, FalloffEnd = 90f },
        [2] = new WeaponDef { Id = 2, Name = "Shotgun",     BaseDamage = 12f, FireInterval = 0.80f, MagSize =  8, Range =  25f, MaxSpreadDeg = 6.0f, FalloffStart =  6f, FalloffEnd = 22f, MinFalloffMult = 0.15f },
        [3] = new WeaponDef { Id = 3, Name = "SniperRifle", BaseDamage = 80f, FireInterval = 1.20f, MagSize =  5, Range = 300f, HeadshotMult = 2.5f, MaxSpreadDeg = 0.1f, FalloffStart = 200f, FalloffEnd = 300f, MinFalloffMult = 0.7f },
        [4] = new WeaponDef { Id = 4, Name = "Splattergun", BaseDamage = 24f, FireInterval = 0.55f, MagSize = 20, Range =  60f, MaxSpreadDeg = 3.0f, FalloffStart = 20f, FalloffEnd = 55f },
    };

    public static WeaponDef Get(int id) => Defs[id];
    public static bool TryGet(int id, out WeaponDef def) => Defs.TryGetValue(id, out def!);
    public static IEnumerable<WeaponDef> All => Defs.Values;
}

/// <summary>Server-side shop. Prices live here; client sends only an item id + idempotency key.</summary>
public static class ShopTable
{
    private static readonly Dictionary<int, long> Prices = new()
    {
        [101] = 500,   // skin
        [102] = 1200,  // weapon mod
        [103] = 2500,  // bundle
    };

    public static bool TryGetPrice(int itemId, out long price) => Prices.TryGetValue(itemId, out price);
}
