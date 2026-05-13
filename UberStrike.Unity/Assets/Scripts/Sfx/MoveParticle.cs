using UnityEngine;
using System.Collections.Generic;

public static class ParticleEmissionSystem
{
    static int _emitLogCount = 0;
    const int MAX_EMIT_LOGS = 30; // enough to see surface + fire + trail emissions

    // TEMP DEBUG: Press F11 to toggle 10x particle size for visual inspection
    public static bool DebugLargeParticles = false;

    // Per-system emission counters for diagnostics
    static Dictionary<string, int> _emitCounts = new Dictionary<string, int>();
    static float _lastCountDump = 0f;

    /// <summary>
    /// Modern Emit using EmitParams — avoids deprecated Emit() which sets applyShapeToPosition=true
    /// and causes shape module (Sphere radius=1 from migration) to offset particle positions.
    /// EmitParams defaults applyShapeToPosition=false so particles spawn at exact position.
    /// </summary>
    public static void EmitSafe(ParticleSystem ps, Vector3 position, Vector3 velocity, float size, float lifetime, Color32 color)
    {
        if (ps == null) return;

        // Ensure system is playing (required for Emit to work on inactive-then-activated GOs)
        if (!ps.isPlaying) ps.Play();

        var ep = new ParticleSystem.EmitParams();
        ep.position = position;
        ep.velocity = velocity;
        // Legacy ParticleRenderer used size as radius, modern ParticleSystemRenderer
        // uses diameter. Per-particle scale factors tuned via frame capture comparison.
        // Round 13: Spark/Trail reduced from 0.5x to 0.35x — the stretched streaks were
        // too prominent post-blast ("expanding electricity" pattern). Smaller = invisible faster.
        float finalSize = DebugLargeParticles ? size * 10f : size;
        string pName = ps.gameObject.name;
        bool isEnigma = ps.transform.parent != null && ps.transform.parent.name == "CNEnigmaCannon";

        if (pName == "ExplosionBlast")
            finalSize *= isEnigma ? 0.85f : 1.2f;  // Enigma: visible nova but not dominant.
        else if (pName == "ExplosionSmoke")
            finalSize *= isEnigma ? 1.2f : 0.7f;   // Enigma: spreading spray with fade. Regular: subtle.
        else if (pName == "ExplosionRing")
            finalSize *= 0.5f;
        else if (pName == "ExplosionSpark" || pName == "ExplosionTrail")
            finalSize *= 0.45f;
        ep.startSize = finalSize;
        ep.startLifetime = lifetime;
        ep.startColor = color;
        ps.Emit(ep, 1);

        // Track per-system emission counts
        string sysName = ps.gameObject.name;
        if (!_emitCounts.ContainsKey(sysName)) _emitCounts[sysName] = 0;
        _emitCounts[sysName]++;

        // Periodic summary instead of per-emission spam
        if (Time.time - _lastCountDump > 5f && _emitCounts.Count > 0)
        {
            _lastCountDump = Time.time;
            string summary = "[EmitSafe STATS] ";
            foreach (var kvp in _emitCounts)
                summary += kvp.Key + "=" + kvp.Value + " ";
            Debug.Log(summary);
        }

        // Detailed verify log for first N emissions
        if (_emitLogCount < MAX_EMIT_LOGS)
        {
            _emitLogCount++;
            int count = ps.particleCount;
            if (count > 0)
            {
                var buf = new ParticleSystem.Particle[count];
                ps.GetParticles(buf, count);
                var p = buf[count - 1];

                Debug.Log("[EmitSafe] " + sysName +
                    " size=" + size.ToString("F3") +
                    " life=" + lifetime.ToString("F2") +
                    " vel=" + velocity.ToString("F1") +
                    " color=" + ((Color)color).ToString() +
                    " pos=" + position.ToString("F1") +
                    " total=" + count);
            }
        }
    }

    public static void TrailParticles(Vector3 emitPoint, Vector3 direction, TrailParticleConfiguration particleConfiguration, Vector3 muzzlePosition, float distance)
    {
        if (particleConfiguration.ParticleEmitter != null && distance > 3f)
        {
            // Exact match of original Unity 3.5.5 TrailParticles code.
            // trace.jpg = thin bright line on black (256x256, no alpha). With Additive shader,
            // only the bright center band renders — black adds nothing. So even with size=5
            // (5m wide quad), the visible trail is just the thin bright line from the texture.
            // Original: Emit(muzzlePosition + direction * 3, velocity, config.Size, time, color)
            float speed = 200f;
            Vector3 velocity = direction * speed;
            float time = distance / speed * 0.9f;

            // Legacy 3.5.5 Stretched particle "size" was radius (half-width),
            // modern ParticleSystem size is diameter (full width) — halve to match original thickness
            EmitSafe(particleConfiguration.ParticleEmitter,
                muzzlePosition + direction * 3f, velocity,
                Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize) * 0.5f,
                time, particleConfiguration.ParticleColor);
        }
    }

    public static void FireParticles(Vector3 hitPoint, Vector3 hitNormal, FireParticleConfiguration particleConfiguration)
    {
        if (particleConfiguration.ParticleEmitter != null)
        {
            Vector3 velocity = Vector3.zero;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hitNormal);
            Vector3 newEmitPoint = Vector3.zero;

            for (int i = 0; i < particleConfiguration.ParticleCount; i++)
            {
                velocity.x = 0 + Random.Range(0f, 0.001f);
                velocity.y = 2f + Random.Range(0f, 0.4f);
                velocity.z = 0 + Random.Range(0f, 0.001f);
                velocity = rotation * velocity;

                newEmitPoint = hitPoint;
                newEmitPoint.x = newEmitPoint.x + Random.Range(0f, 0.2f);
                newEmitPoint.z = newEmitPoint.z + Random.Range(0f, 0.4f) * (-1);
                EmitSafe(particleConfiguration.ParticleEmitter,
                    newEmitPoint, velocity,
                    Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize),
                    Random.Range(particleConfiguration.ParticleMinLiveTime, particleConfiguration.ParticleMaxLiveTime),
                    particleConfiguration.ParticleColor);
            }
        }
    }

    public static void WaterCircleParticles(Vector3 hitPoint, Vector3 hitNormal, FireParticleConfiguration particleConfiguration)
    {
        if (particleConfiguration.ParticleEmitter != null)
        {
            Vector3 velocity = Vector3.zero;
            for (int i = 0; i < particleConfiguration.ParticleCount; i++)
            {
                velocity.x = Random.Range(0f, 0.3f);
                velocity.z = Random.Range(0f, 0.3f);

                EmitSafe(particleConfiguration.ParticleEmitter,
                    hitPoint, velocity,
                    Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize),
                    Random.Range(particleConfiguration.ParticleMinLiveTime, particleConfiguration.ParticleMaxLiveTime),
                    particleConfiguration.ParticleColor);
            }
        }
    }

    public static void WaterSplashParticles(Vector3 hitPoint, Vector3 hitNormal, FireParticleConfiguration particleConfiguration)
    {
        if (particleConfiguration.ParticleEmitter != null)
        {
            Vector3 velocity = Vector3.zero;
            for (int i = 0; i < particleConfiguration.ParticleCount; i++)
            {
                velocity.x = Random.Range(0f, 0.3f);
                velocity.y = 2 + Random.Range(0f, 0.3f);
                velocity.z = Random.Range(0f, 0.3f);

                EmitSafe(particleConfiguration.ParticleEmitter,
                    hitPoint, velocity,
                    Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize),
                    Random.Range(particleConfiguration.ParticleMinLiveTime, particleConfiguration.ParticleMaxLiveTime),
                    particleConfiguration.ParticleColor);
            }
        }
    }


    public static void HitMaterialParticles(Vector3 hitPoint, Vector3 hitNormal, ParticleConfiguration particleConfiguration)
    {
        if (particleConfiguration.ParticleEmitter != null)
        {
            Vector3 velocity = Vector3.zero;
            Vector2 unitCircleTrajectory;
            Quaternion rotation = new Quaternion();
            rotation = Quaternion.FromToRotation(Vector3.back, hitNormal);

            for (int i = 0; i < particleConfiguration.ParticleCount; i++)
            {
                unitCircleTrajectory = Random.insideUnitCircle * Random.Range(particleConfiguration.ParticleMinSpeed, particleConfiguration.ParticleMaxSpeed);
                velocity.x = unitCircleTrajectory.x;
                velocity.y = unitCircleTrajectory.y;
                velocity.z = Random.Range(particleConfiguration.ParticleMinZVelocity, particleConfiguration.ParticleMaxZVelocity) * (-1);// maxSpeed * 4; //(j + 1) * -2; //Random.Range(5f * (float)(j + 1), 10f * (float)(j + 1)) * (-1);
                velocity = rotation * velocity;
                EmitSafe(particleConfiguration.ParticleEmitter,
                    hitPoint, velocity,
                    Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize),
                    Random.Range(particleConfiguration.ParticleMinLiveTime, particleConfiguration.ParticleMaxLiveTime),
                    particleConfiguration.ParticleColor);
            }
        }
    }

    public static void HitMaterialRotatingParticles(Vector3 hitPoint, Vector3 hitNormal, ParticleConfiguration particleConfiguration)
    {
        if (particleConfiguration.ParticleEmitter != null)
        {
            var ps = particleConfiguration.ParticleEmitter;
            if (!ps.isPlaying) ps.Play();

            Vector3 velocity = Vector3.zero;
            Vector2 unitCircleTrajectory;
            Quaternion rotation = new Quaternion();
            rotation = Quaternion.FromToRotation(Vector3.back, hitNormal);

            for (int i = 0; i < particleConfiguration.ParticleCount; i++)
            {
                unitCircleTrajectory = Random.insideUnitCircle * Random.Range(particleConfiguration.ParticleMinSpeed, particleConfiguration.ParticleMaxSpeed);
                velocity.x = unitCircleTrajectory.x;
                velocity.y = unitCircleTrajectory.y;
                velocity.z = Random.Range(particleConfiguration.ParticleMinZVelocity, particleConfiguration.ParticleMaxZVelocity) * (-1);
                velocity = rotation * velocity;
                var emitParams = new ParticleSystem.EmitParams();
                emitParams.position = hitPoint;
                emitParams.velocity = velocity;
                emitParams.startSize = DebugLargeParticles ?
                    Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize) * 10f :
                    Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize);
                emitParams.startLifetime = Random.Range(particleConfiguration.ParticleMinLiveTime, particleConfiguration.ParticleMaxLiveTime);
                emitParams.startColor = particleConfiguration.ParticleColor;
                emitParams.rotation = Random.Range(0f, 360f);
                ps.Emit(emitParams, 1);
            }
        }
    }

    public static void HitMateriaHalfSphericParticles(Vector3 hitPoint, Vector3 hitNormal, ParticleConfiguration particleConfiguration)
    {
        if (particleConfiguration.ParticleEmitter != null)
        {
            Vector3 velocity = Vector3.zero;
            Quaternion rotation = new Quaternion();
            rotation = Quaternion.FromToRotation(Vector3.back, hitNormal);

            for (int i = 0; i < particleConfiguration.ParticleCount; i++)
            {
                velocity = Random.insideUnitSphere * Random.Range(particleConfiguration.ParticleMinSpeed, particleConfiguration.ParticleMaxSpeed);
                if (velocity.z > 0) velocity.z = velocity.z * (-1);
                velocity = rotation * velocity;
                EmitSafe(particleConfiguration.ParticleEmitter,
                    hitPoint, velocity,
                    Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize),
                    Random.Range(particleConfiguration.ParticleMinLiveTime, particleConfiguration.ParticleMaxLiveTime),
                    particleConfiguration.ParticleColor);
            }
        }
    }

    public static void HitMateriaFullSphericParticles(Vector3 hitPoint, Vector3 hitNormal, ParticleConfiguration particleConfiguration)
    {
        if (particleConfiguration.ParticleEmitter != null)
        {
            Vector3 velocity = Vector3.zero;

            for (int i = 0; i < particleConfiguration.ParticleCount; i++)
            {
                velocity = Random.insideUnitSphere * Random.Range(particleConfiguration.ParticleMinSpeed, particleConfiguration.ParticleMaxSpeed);
                EmitSafe(particleConfiguration.ParticleEmitter,
                    hitPoint, velocity,
                    Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize),
                    Random.Range(particleConfiguration.ParticleMinLiveTime, particleConfiguration.ParticleMaxLiveTime),
                    particleConfiguration.ParticleColor);
            }
        }
    }
}
