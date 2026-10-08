namespace UberStrike.Server;

/// <summary>
/// Phase 9 — client build-hash gate. The host computes a hash of the shipped WebGL build
/// (e.g. the .wasm + .data) at deploy time and registers the allowed value(s). On connect, the
/// client reports the hash of the build it's running; a mismatch is a SOFT signal (telemetry +
/// anomaly), never a hard ban — a determined attacker can forge it, but it cheaply catches stale
/// builds and naive tampering, and it's the hook the host uses to force-update outdated clients.
///
/// Honest framing (server-authority.md §9): client hardening is defense-in-depth, NOT a
/// substitute for server authority. Nothing here is trusted as security; the server still owns
/// every outcome regardless of what the client reports.
/// </summary>
public sealed class IntegrityRegistry
{
    private readonly HashSet<string> _allowed = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Register an accepted build hash (current + the previous during a rollout window).</summary>
    public void Allow(string buildHash)
    {
        if (!string.IsNullOrWhiteSpace(buildHash)) _allowed.Add(buildHash.Trim());
    }

    public bool HasAllowList => _allowed.Count > 0;

    /// <summary>True if the reported hash is accepted. With no allow-list configured, accepts all
    /// (don't lock players out before the host has wired deploy-time hashing).</summary>
    public bool IsAccepted(string reportedHash)
        => !HasAllowList || (_reportedOk(reportedHash));

    private bool _reportedOk(string reportedHash)
        => !string.IsNullOrWhiteSpace(reportedHash) && _allowed.Contains(reportedHash.Trim());
}
