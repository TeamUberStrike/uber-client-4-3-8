using System.Diagnostics;
using System.Numerics;
using UberStrike.Shared;

namespace UberStrike.Server;

/// <summary>
/// Phase 10 — one independent authoritative simulation. Rooms never share state, so they shard
/// trivially across threads/processes. Owns its own entity-id allocation.
/// </summary>
public sealed class Room
{
    public int Id { get; }
    public int Capacity { get; }
    public ServerSimulation Sim { get; }
    private int _nextEntity = 1;

    public Room(int id, ICollisionWorld world, int capacity,
                Action<int, Snapshot> send, Action<HitEvent> broadcast)
    {
        Id = id; Capacity = capacity;
        Sim = new ServerSimulation(world, send, broadcast);
    }

    public int PlayerCount => Sim.PlayerCount;
    public bool IsFull => Sim.PlayerCount >= Capacity;
    public bool IsEmpty => Sim.PlayerCount == 0;

    public bool TryAdd(string token, Vector3 spawn, int weaponId, out int entityId)
    {
        entityId = -1;
        if (IsFull) return false;
        entityId = _nextEntity++;
        Sim.AddPlayer(entityId, token, spawn, weaponId);
        return true;
    }

    public bool Remove(int entityId) => Sim.RemovePlayer(entityId);
}

/// <summary>
/// Phase 10 — match sharding + ops. Holds many independent rooms, routes joins to a room with
/// space (or a new one), reaps empty rooms, and times the per-room tick loop into TickMetrics.
/// Single-threaded StepAll here (deterministic + simple); because rooms share nothing, a host
/// can fan StepAll across a thread pool unchanged.
/// </summary>
public sealed class MatchManager
{
    private readonly Func<ICollisionWorld> _worldFactory;
    private readonly int _roomCapacity;
    private readonly Dictionary<int, Room> _rooms = new();
    private int _nextRoomId = 1;

    public TickMetrics Metrics { get; } = new();

    public MatchManager(Func<ICollisionWorld> worldFactory, int roomCapacity = 12)
    {
        _worldFactory = worldFactory;
        _roomCapacity = roomCapacity;
    }

    public int RoomCount => _rooms.Count;
    public IReadOnlyCollection<Room> Rooms => _rooms.Values;

    public Room CreateRoom(Action<int, Snapshot>? send = null, Action<HitEvent>? broadcast = null)
    {
        int id = _nextRoomId++;
        var room = new Room(id, _worldFactory(), _roomCapacity,
            send ?? ((_, _) => { }), broadcast ?? (_ => { }));
        _rooms[id] = room;
        return room;
    }

    /// <summary>Join an existing room with space, else spin up a new one. Returns (room, entityId).</summary>
    public (Room room, int entityId) Join(string token, Vector3 spawn, int weaponId,
        Action<int, Snapshot>? send = null, Action<HitEvent>? broadcast = null)
    {
        foreach (Room r in _rooms.Values)
            if (!r.IsFull && r.TryAdd(token, spawn, weaponId, out int eid))
                return (r, eid);

        Room nr = CreateRoom(send, broadcast);
        nr.TryAdd(token, spawn, weaponId, out int neid);
        return (nr, neid);
    }

    public void ReapEmptyRooms()
    {
        List<int>? dead = null;
        foreach (Room r in _rooms.Values) if (r.IsEmpty) (dead ??= new()).Add(r.Id);
        if (dead != null) foreach (int id in dead) _rooms.Remove(id);
    }

    /// <summary>Tick every room once, recording per-room tick time into <see cref="Metrics"/>.</summary>
    public void StepAll()
    {
        foreach (Room r in _rooms.Values)
        {
            long t0 = Stopwatch.GetTimestamp();
            r.Sim.StepTick();
            double ms = (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
            Metrics.RecordTick(ms);
        }
    }
}
