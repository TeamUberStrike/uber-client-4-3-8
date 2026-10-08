#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using UnityEngine;
using UberStrike.Shared;
using UberStrike.Client;

namespace UberStrike.Netcode.Unity
{
    /// <summary>
    /// Phase 3 — renders OTHER players from snapshots, in the buffered past
    /// (renderTime = ServerNow − InterpDelay) via RemoteInterpolator. Because the server's
    /// Fog of War (Phase 5.5) omits players the local viewer can't see, an entity can stop
    /// appearing in snapshots at any time: this manager hides an avatar whose interpolation
    /// buffer has gone stale and re-shows it when it reappears (its buffer simply starves;
    /// see RemoteInterpolator's extrapolate-then-freeze). It never invents a position for a
    /// culled enemy — that's the whole point of the cull.
    /// </summary>
    public sealed class RemotePlayerManager : MonoBehaviour
    {
        [SerializeField] private GameObject _remoteAvatarPrefab;

        private readonly Dictionary<int, RemoteInterpolator> _interp = new();
        private readonly Dictionary<int, Transform> _avatars = new();
        private readonly Dictionary<int, double> _lastSeen = new();
        private ClockSync _clock;

        /// <summary>Hide an avatar after this long with no fresh snapshot (fog-of-war cull / loss).</summary>
        private const double HideAfterSeconds = 0.5;

        public void Init(ClockSync clock) => _clock = clock;

        /// <summary>Call from the snapshot handler with the same Snapshot fed to the local player.</summary>
        public void OnSnapshot(in Snapshot s)
        {
            foreach (PlayerSnap o in s.Others)
            {
                if (!_interp.TryGetValue(o.EntityId, out var ri)) { ri = new RemoteInterpolator(); _interp[o.EntityId] = ri; }
                ri.Ingest(s.ServerTime, ToCore(o.Position), o.Yaw, o.Pitch);
                _lastSeen[o.EntityId] = s.ServerTime;
                EnsureAvatar(o.EntityId).gameObject.SetActive(true);
            }
        }

        private void Update()
        {
            if (_clock == null) return;
            double renderTime = _clock.ServerNow(Time.timeAsDouble) - GameConstants.InterpDelaySeconds;

            foreach (var kv in _avatars)
            {
                int id = kv.Key;
                Transform t = kv.Value;
                bool stale = !_lastSeen.TryGetValue(id, out double seen)
                             || _clock.ServerNow(Time.timeAsDouble) - seen > HideAfterSeconds;
                if (stale) { t.gameObject.SetActive(false); continue; }   // fog-of-war: nothing to render

                if (_interp[id].Sample(renderTime, out var pos, out float yaw, out float pitch))
                {
                    t.position = NetcodeBoundary.ToUnity(pos);
                    t.rotation = Quaternion.Euler(0f, NetcodeBoundary.RadToDeg(yaw), 0f);
                }
            }
        }

        private Transform EnsureAvatar(int id)
        {
            if (_avatars.TryGetValue(id, out Transform t)) return t;
            GameObject go = _remoteAvatarPrefab != null ? Instantiate(_remoteAvatarPrefab) : new GameObject($"Remote_{id}");
            _avatars[id] = go.transform;
            return go.transform;
        }

        private static System.Numerics.Vector3 ToCore(Vector3 v) => NetcodeBoundary.ToCore(v);
    }
}
#endif
