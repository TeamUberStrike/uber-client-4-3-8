using System.Numerics;
using UberStrike.Shared;

namespace UberStrike.Server;

/// <summary>
/// Fog of War — per-viewer relevance culling, the ESP/wallhack defense (VALORANT model:
/// don't network what the viewer can't see; information the client never received cannot
/// be read out of WASM memory). Runs server-side only and never touches the shared
/// simulation, so client/server determinism is unaffected.
///
/// A target is sent to a viewer when ANY of:
///   - same team (teammates are always relevant; no information advantage to cheat),
///   - the viewer is dead (spectate/kill-cam needs the world; respawn is seconds away),
///   - the target is dead (the kill event already broadcast that position),
///   - the target fired recently (gunfire is audible and traced in-world anyway),
///   - a sightline is clear from the viewer's eye to any body sample point (head, torso,
///     feet, both shoulders) of the target — at its current position OR at its position
///     extrapolated by velocity ("looking into the future", so a peek is revealed before
///     it renders on the peeker's screen instead of popping in late),
///   - LOS was clear within the last <see cref="GameConstants.VisGraceSeconds"/>
///     (hysteresis: covers the remote-interpolation delay and kills edge flicker).
///
/// Cost: O(viewers × targets) pair checks, ≤8 BVH raycasts each, evaluated lazily
/// (first clear sample wins) — trivial at 30 Hz for UberStrike room sizes.
/// </summary>
public sealed class VisibilitySystem
{
    private readonly ICollisionWorld _world;
    private readonly Dictionary<(int viewer, int target), double> _lastVisible = new();

    public VisibilitySystem(ICollisionWorld world) => _world = world;

    public bool ShouldSend(PlayerState viewer, PlayerState target, double serverNow)
    {
        if (viewer.TeamId != 0 && viewer.TeamId == target.TeamId) return true;
        if (!viewer.Alive || !target.Alive) return true;

        if (serverNow - target.LastFireTime <= GameConstants.FireRevealSeconds)
        {
            _lastVisible[(viewer.EntityId, target.EntityId)] = serverNow;
            return true;
        }

        if (HasLineOfSight(viewer, target))
        {
            _lastVisible[(viewer.EntityId, target.EntityId)] = serverNow;
            return true;
        }

        return _lastVisible.TryGetValue((viewer.EntityId, target.EntityId), out double last)
            && serverNow - last <= GameConstants.VisGraceSeconds;
    }

    /// <summary>Drop a disconnected player's pair history (both as viewer and as target).</summary>
    public void RemovePlayer(int entityId)
    {
        List<(int, int)>? stale = null;
        foreach ((int v, int t) key in _lastVisible.Keys)
            if (key.Item1 == entityId || key.Item2 == entityId)
                (stale ??= new List<(int, int)>()).Add(key);
        if (stale != null) foreach ((int, int) key in stale) _lastVisible.Remove(key);
    }

    private bool HasLineOfSight(PlayerState viewer, PlayerState target)
    {
        Vector3 eye = viewer.Move.Position + new Vector3(0f,
            viewer.Move.Ducked ? GameConstants.HeightDucked : GameConstants.EyeHeight, 0f);

        if (AnySampleVisible(eye, viewer.Move.Position, target.Move.Position)) return true;

        // "Look into the future": test the target where it will be in VisLookaheadSeconds,
        // so a fast peek is already streaming to the viewer when it rounds the corner.
        Vector3 ahead = target.Move.Position + target.Move.Velocity * GameConstants.VisLookaheadSeconds;
        return AnySampleVisible(eye, viewer.Move.Position, ahead);
    }

    private bool AnySampleVisible(Vector3 eye, Vector3 viewerFeet, Vector3 targetFeet)
    {
        const float torsoMid = (GameConstants.BodyBottom + GameConstants.BodyTop) * 0.5f;

        // vertical samples: a head peeking over a ledge or legs under a gap is enough
        if (_world.LineOfSight(eye, targetFeet + new Vector3(0f, GameConstants.HeadOffset, 0f))) return true;
        if (_world.LineOfSight(eye, targetFeet + new Vector3(0f, torsoMid, 0f))) return true;
        if (_world.LineOfSight(eye, targetFeet + new Vector3(0f, GameConstants.BodyBottom + 0.05f, 0f))) return true;

        // lateral samples at torso height: a shoulder past a corner reveals
        Vector3 to = targetFeet - viewerFeet; to.Y = 0f;
        float len = to.Length();
        if (len > 1e-4f)
        {
            Vector3 side = new Vector3(-to.Z / len, 0f, to.X / len) * GameConstants.BodyRadius;
            Vector3 torso = targetFeet + new Vector3(0f, torsoMid, 0f);
            if (_world.LineOfSight(eye, torso + side)) return true;
            if (_world.LineOfSight(eye, torso - side)) return true;
        }
        return false;
    }
}
