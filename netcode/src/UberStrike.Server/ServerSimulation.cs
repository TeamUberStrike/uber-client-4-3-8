using System.Numerics;
using UberStrike.Shared;

namespace UberStrike.Server;

/// <summary>
/// Fixed-tick authoritative simulation. Order per tick:
///   1) consume one validated input per player -> movement guard
///   2) record each player's hitbox at this server time (for later rewind)
///   3) resolve queued fire intents (lag-comp uses the history from step 2)
///   4) decay anomaly scores
///   5) emit a per-player snapshot (own state + others)
/// </summary>
public sealed class ServerSimulation
{
    private readonly ICollisionWorld _world;
    private readonly MovementSystem  _movement;
    private readonly CombatSystem    _combat;
    public  readonly EconomySystem   Economy = new();

    private readonly Dictionary<int, PlayerState>     _players     = new();
    private readonly Dictionary<int, Queue<InputCmd>> _inputQueues = new();
    private readonly Queue<(int entity, FireIntent intent)> _fireQueue   = new();
    private readonly Queue<SwitchIntent>                    _switchQueue = new();
    private readonly List<Projectile>                       _projectiles = new();

    private readonly Action<int, Snapshot> _sendSnapshot;
    private readonly Action<HitEvent>      _broadcast;

    public double ServerTime { get; private set; }
    public uint   Tick { get; private set; }

    public ServerSimulation(ICollisionWorld world, Action<int, Snapshot> sendSnapshot, Action<HitEvent> broadcast)
    {
        _world = world;
        _sendSnapshot = sendSnapshot;
        _broadcast = broadcast;
        _movement = new MovementSystem(world);
        _combat   = new CombatSystem(world, () => _players.Values, h => _broadcast(h));
    }

    public IEnumerable<PlayerState> Players => _players.Values;
    public PlayerState? Get(int id) => _players.TryGetValue(id, out PlayerState? p) ? p : null;

    public PlayerState AddPlayer(int id, string token, Vector3 spawn, int weaponId)
    {
        WeaponDef def = WeaponTable.Get(weaponId);
        PlayerState p = new()
        {
            EntityId = id,
            SessionToken = token,
            ActiveSlot = 0,
            Weapons = new[] { new WeaponRuntime { WeaponId = weaponId, Ammo = def.MagSize, Reserve = def.MagSize * 3 } },
        };
        p.Move.Position = spawn;
        p.Move.Grounded = true;
        _players[id] = p;
        _inputQueues[id] = new Queue<InputCmd>();
        return p;
    }

    /// <summary>Gateway entry point. Returns false if the packet was rejected.</summary>
    public bool EnqueueInput(in InputPacket packet)
    {
        if (!_players.TryGetValue(packet.EntityId, out PlayerState? s)) return false;
        if (!InputGateway.Validate(s, packet, out _)) return false;
        _inputQueues[packet.EntityId].Enqueue(packet.Cmd);
        return true;
    }

    /// <summary>
    /// Fire intents pass the same ownership check as inputs. Without this, ANY connected
    /// client could fire ANOTHER player's weapon by forging EntityId (found in Phase A audit).
    /// </summary>
    public bool EnqueueFire(in FireIntent f)
    {
        if (!_players.TryGetValue(f.EntityId, out PlayerState? s)) return false;
        if (f.SessionToken != s.SessionToken)
        {
            s.Anomaly.Bump(AnomalyKind.SchemaViolation, 0.5f);
            return false;
        }
        _fireQueue.Enqueue((f.EntityId, f));
        return true;
    }

    /// <summary>Weapon-switch intent with the same ownership check as inputs/fire.</summary>
    public bool EnqueueSwitch(in SwitchIntent sw)
    {
        if (!_players.TryGetValue(sw.EntityId, out PlayerState? s)) return false;
        if (sw.SessionToken != s.SessionToken) { s.Anomaly.Bump(AnomalyKind.SchemaViolation, 0.5f); return false; }
        _switchQueue.Enqueue(sw);
        return true;
    }

    public IReadOnlyList<Projectile> Projectiles => _projectiles;

    public void StepTick()
    {
        Tick++;
        ServerTime = Tick * GameConstants.FixedDt;

        // 1 + 2: movement, then record hitbox history at this tick.
        foreach (KeyValuePair<int, PlayerState> kv in _players)
        {
            PlayerState s = kv.Value;
            Queue<InputCmd> q = _inputQueues[kv.Key];
            if (q.Count > 0) _movement.Apply(s, q.Dequeue(), GameConstants.FixedDt);
            s.History.Record(ServerTime, s.Move.Position, s.Move.Yaw);
        }

        // 3a: weapon switches before fires this tick (so a switch+fire in one tick is gated).
        while (_switchQueue.Count > 0)
        {
            SwitchIntent sw = _switchQueue.Dequeue();
            if (_players.TryGetValue(sw.EntityId, out PlayerState? sp) && sp.Alive)
                _combat.HandleSwitch(sp, sw.Slot, ServerTime);
        }

        // 3b: combat (uses the freshly recorded history for lag-comp rewind). A projectile
        //     weapon returns a round to track; hitscan/shotgun resolve inline.
        while (_fireQueue.Count > 0)
        {
            (int id, FireIntent intent) = _fireQueue.Dequeue();
            if (_players.TryGetValue(id, out PlayerState? shooter) && shooter.Alive)
            {
                Projectile? proj = _combat.HandleFire(shooter, intent, ServerTime);
                if (proj != null) _projectiles.Add(proj);
            }
        }

        // 3c: advance projectiles; drop spent ones.
        for (int i = _projectiles.Count - 1; i >= 0; i--)
            if (!_combat.StepProjectile(_projectiles[i], GameConstants.FixedDt, ServerTime))
                _projectiles.RemoveAt(i);

        // 4: anomaly decay.
        foreach (PlayerState s in _players.Values) s.Anomaly.Decay(GameConstants.FixedDt);

        // 5: snapshots.
        foreach (KeyValuePair<int, PlayerState> kv in _players)
            _sendSnapshot(kv.Key, BuildSnapshot(kv.Value));
    }

    private Snapshot BuildSnapshot(PlayerState me)
    {
        List<PlayerSnap> others = new(Math.Max(0, _players.Count - 1));
        foreach (PlayerState p in _players.Values)
            if (!ReferenceEquals(p, me)) others.Add(ToSnap(p));

        return new Snapshot
        {
            ServerTime = ServerTime,
            LastProcessedInput = me.LastProcessedInput,
            Local = ToSnap(me),
            Others = others.ToArray(),
        };
    }

    private static PlayerSnap ToSnap(PlayerState p) => new()
    {
        EntityId = p.EntityId,
        Position = p.Move.Position,
        Velocity = p.Move.Velocity,
        Yaw = p.Move.Yaw,
        Pitch = p.Move.Pitch,
        Grounded = p.Move.Grounded,
        Jumping = p.Move.Jumping,
        Ducked = p.Move.Ducked,
        JumpArmed = p.Move.JumpArmed,
        UngroundedTicks = p.Move.UngroundedTicks,
        SpeedScale = p.Move.SpeedScale,
        Health = p.Health,
        ActiveSlot = p.ActiveSlot,
        ActiveAmmo = p.ActiveWeapon.Ammo,
    };
}
