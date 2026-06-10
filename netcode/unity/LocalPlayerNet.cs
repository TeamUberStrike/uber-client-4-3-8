#if UNITY_5_3_OR_NEWER
using UnityEngine;
using UberStrike.Shared;
using UberStrike.Client;

namespace UberStrike.Netcode.Unity
{
    /// <summary>
    /// Phase 3 — MonoBehaviour adapter driving the local player through the netcode client core.
    /// Fixed-step input sampling → BuildAndPredict → send; per-frame error smoothing onto the
    /// transform; Reconcile on each authoritative snapshot. NO game rules live here — the core
    /// owns prediction, the server owns outcomes. This is the contract the WASM build runs;
    /// in-engine verification (smooth local motion, imperceptible corrections) is the Phase-3
    /// done-when and needs the Unity project, not this repo.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalPlayerNet : MonoBehaviour
    {
        private PredictionClient _pred;
        private CombatClient     _combat;
        private readonly ClockSync _clock = new();
        private IClientLink      _link;        // WebSocketClientLink in production
        private ICollisionWorld  _world;       // BakedCollisionWorld loaded from the map's .ubw

        private float _accum;
        private uint  _tick;
        private double _lastPingAt;

        /// <summary>Host wires the link + loaded world + authenticated identity after Welcome.</summary>
        public void Init(IClientLink link, ICollisionWorld world, int entityId, string token, Vector3 spawn)
        {
            _link  = link;
            _world = world;
            _pred  = new PredictionClient(world, entityId, token, NetcodeBoundary.ToCore(spawn));
            // weapon/mag/fireinterval come from the authoritative table the server also uses
            WeaponDef def = WeaponTable.Get(1);
            _combat = new CombatClient(entityId, token, 0, def.MagSize, def.FireInterval);
            link.SnapshotReceived += OnSnapshot;
            link.HitReceived      += e => _combat.OnHitEvent(e);
        }

        private void FixedUpdate()
        {
            if (_pred == null) return;
            _accum += Time.fixedDeltaTime;
            while (_accum >= GameConstants.FixedDt)
            {
                _accum -= GameConstants.FixedDt;

                var cmd = SampleInput();
                _link.SendInput(_pred.BuildAndPredict(cmd));

                if (Input.GetButton("Fire1"))
                {
                    var fi = _combat.TryFire(Time.timeAsDouble, _tick);
                    if (fi.HasValue) _link.SendFire(fi.Value);
                }
                _tick++;
            }

            // periodic clock-sync ping (server answers Pong → ClockSync → remote interp + RTT)
            if (Time.timeAsDouble - _lastPingAt > 1.0 && _link is WebSocketClientLink ws)
            {
                _lastPingAt = Time.timeAsDouble;
                ws.SendPing(Time.timeAsDouble);
            }
        }

        private void Update()
        {
            if (_pred == null) return;
            _pred.SmoothErrors(Time.deltaTime);
            transform.position = NetcodeBoundary.ToUnity(_pred.RenderPosition);
        }

        private void OnSnapshot(Snapshot s)
        {
            _pred.Reconcile(s);
            _combat.ReconcileAmmo(s.Local.ActiveAmmo);
        }

        private InputCmd SampleInput()
        {
            float mx = Input.GetAxisRaw("Horizontal");
            float mz = Input.GetAxisRaw("Vertical");
            var dir = new System.Numerics.Vector3(mx, 0f, mz);
            return new InputCmd
            {
                ClientTick = _tick,
                MoveDir = dir,                                    // core clamps magnitude
                Jump   = Input.GetButton("Jump"),
                Crouch = Input.GetButton("Crouch"),
                Yaw    = NetcodeBoundary.DegToRad(transform.eulerAngles.y),
                Pitch  = NetcodeBoundary.DegToRad(-Camera.main.transform.eulerAngles.x),
            };
        }
    }
}
#endif
