using System.Numerics;
using UberStrike.Shared;

namespace UberStrike.Server;

/// <summary>
/// First line of defense. Every inbound packet passes here before any system touches state:
/// auth binding, schema/range, replay/ordering. (Rate limiting is enforced at the transport
/// layer in production; see IMPLEMENTATION_PLAN.md Phase 7.)
/// </summary>
public static class InputGateway
{
    public static bool Validate(PlayerState s, in InputPacket p, out string reason)
    {
        // 1. Auth: packet must belong to the entity this session controls.
        if (p.EntityId != s.EntityId || p.SessionToken != s.SessionToken)
        {
            reason = "auth"; s.Anomaly.Bump(AnomalyKind.SchemaViolation, 0.5f); return false;
        }

        InputCmd c = p.Cmd;

        // 2. Schema / range.
        if (!IsFinite(c.MoveDir) || c.MoveDir.LengthSquared() > 1.01f || MathF.Abs(c.Pitch) > 1.58f)
        {
            reason = "schema"; s.Anomaly.Bump(AnomalyKind.SchemaViolation); return false;
        }

        // 3. Replay / ordering: strictly increasing within a bounded window.
        if (c.Seq <= s.LastSeenInputSeq || c.Seq > s.LastSeenInputSeq + GameConstants.MaxSeqGap)
        {
            reason = "seq"; return false;
        }

        s.LastSeenInputSeq = c.Seq;
        reason = "";
        return true;
    }

    private static bool IsFinite(Vector3 v) =>
        float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
}
