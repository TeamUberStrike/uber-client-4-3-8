#if UNITY_5_3_OR_NEWER
using SN = System.Numerics;
using UnityEngine;

namespace UberStrike.Netcode.Unity
{
    /// <summary>
    /// Phase 3 — the ONE place System.Numerics ↔ UnityEngine vectors convert. The netcode core
    /// (UberStrike.Client / .Shared) is engine-agnostic and uses System.Numerics so it unit-tests
    /// without Unity; everything crossing into the engine goes through here. Keeping the boundary
    /// in a single file makes the "no UnityEngine types leak into the deterministic core" rule
    /// auditable.
    /// </summary>
    public static class NetcodeBoundary
    {
        public static SN.Vector3 ToCore(Vector3 v) => new(v.x, v.y, v.z);
        public static Vector3    ToUnity(SN.Vector3 v) => new(v.X, v.Y, v.Z);

        // UberStrike yaw/pitch are radians; the core's DirFromAngles is +Z-forward at yaw 0.
        public static float DegToRad(float deg) => deg * Mathf.Deg2Rad;
        public static float RadToDeg(float rad) => rad * Mathf.Rad2Deg;
    }
}
#endif
