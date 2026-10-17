# Unity integration (adapter layer)

The client core (`UberStrike.Client`) is engine-agnostic and uses `System.Numerics.Vector3`
so it builds and unit-tests without Unity. In the actual game, a thin MonoBehaviour adapter
owns the loop and converts at the boundary.

## Boundary conversions

```csharp
using SN = System.Numerics;
static SN.Vector3 ToCore(UnityEngine.Vector3 v) => new(v.x, v.y, v.z);
static UnityEngine.Vector3 ToUnity(SN.Vector3 v) => new(v.X, v.Y, v.Z);
```

## Local player driver (sketch)

```csharp
public class LocalPlayerNet : MonoBehaviour
{
    PredictionClient _pred;
    CombatClient     _combat;
    ClockSync        _clock = new();
    IClientLink      _link;     // WebSocket implementation in production
    float _accum;

    void FixedUpdate()
    {
        _accum += Time.fixedDeltaTime;
        while (_accum >= GameConstants.FixedDt)
        {
            _accum -= GameConstants.FixedDt;
            var cmd = SampleInput();                 // WASD/stick + look
            _link.SendInput(_pred.BuildAndPredict(cmd));
            if (Input.GetButton("Fire1"))
            {
                var fi = _combat.TryFire(Time.timeAsDouble, _tick);
                if (fi.HasValue) _link.SendFire(fi.Value);
            }
            _tick++;
        }
    }

    void Update()
    {
        _pred.SmoothErrors(Time.deltaTime);
        transform.position = ToUnity(_pred.RenderPosition);   // smoothed predicted position
    }

    void OnSnapshot(Snapshot s) { _pred.Reconcile(s); _combat.ReconcileAmmo(s.Local.ActiveAmmo); }
}
```

## Remote players

Feed each remote `PlayerSnap` into a `RemoteInterpolator` and sample at
`renderTime = clock.ServerNow(now) - GameConstants.InterpDelaySeconds` every frame, then
write the result to the remote avatar's transform.

> IL2CPP/WebGL note: keep `SharedMovement` and `Geometry` off any platform-divergent float
> behavior — that code is compiled to WASM on the client and runs as native C# on the server,
> and the two must agree. See IMPLEMENTATION_PLAN.md, Phase 1 & 9.
