using UnityEngine;

public static class ParticleEmissionSystem
{
    public static void TrailParticles(Vector3 emitPoint, Vector3 direction, TrailParticleConfiguration particleConfiguration, Vector3 muzzlePosition, float distance)
    {
        if (particleConfiguration.ParticleEmitter != null)
        {
            float speed = 200f;
            Vector3 velocity = direction * speed;
            float time = distance / speed * 0.9f;

            if (distance > 3f)
            {
                particleConfiguration.ParticleEmitter.Emit(muzzlePosition + direction * 3, velocity, Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize), time, particleConfiguration.ParticleColor);
            }
        }
    }

    public static void FireParticles(Vector3 hitPoint, Vector3 hitNormal, FireParticleConfiguration particleConfiguration)
    {
        if (particleConfiguration.ParticleEmitter != null)
        {
            Vector3 velocity = Vector3.zero;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hitNormal);
            Vector3 newEmitPoint = Vector3.zero;
            var emitParams = new ParticleSystem.EmitParams();

            for (int i = 0; i < particleConfiguration.ParticleCount; i++)
            {
                velocity.x = 0 + Random.Range(0f, 0.001f);
                velocity.y = 2f + Random.Range(0f, 0.4f);
                velocity.z = 0 + Random.Range(0f, 0.001f);
                velocity = rotation * velocity;

                newEmitPoint = hitPoint;
                newEmitPoint.x = newEmitPoint.x + Random.Range(0f, 0.2f);
                newEmitPoint.z = newEmitPoint.z + Random.Range(0f, 0.4f) * (-1);
                
                emitParams.position = newEmitPoint;
                emitParams.velocity = velocity;
                emitParams.startSize = Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize);
                emitParams.startLifetime = Random.Range(particleConfiguration.ParticleMinLiveTime, particleConfiguration.ParticleMaxLiveTime);
                emitParams.startColor = particleConfiguration.ParticleColor;
                particleConfiguration.ParticleEmitter.Emit(emitParams, 1);
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

                particleConfiguration.ParticleEmitter.Emit(hitPoint, velocity,
                    Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize),
                    Random.Range(particleConfiguration.ParticleMinLiveTime, particleConfiguration.ParticleMaxLiveTime), particleConfiguration.ParticleColor);
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

                particleConfiguration.ParticleEmitter.Emit(hitPoint, velocity,
                    Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize),
                    Random.Range(particleConfiguration.ParticleMinLiveTime, particleConfiguration.ParticleMaxLiveTime), particleConfiguration.ParticleColor);
            }
        }
    }


    public static void HitMaterialParticles(Vector3 hitPoint, Vector3 hitNormal, ParticleConfiguration particleConfiguration)
    {
        Debug.Log($"[HitMaterialParticles] CALLED! ParticleEmitter is null: {particleConfiguration.ParticleEmitter == null}");
        
        if (particleConfiguration.ParticleEmitter != null)
        {
            Vector3 velocity = Vector3.zero;
            Vector2 unitCircleTrajectory;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.back, hitNormal);

            // Modern API: Use EmitParams
            var emitParams = new ParticleSystem.EmitParams();
            
            for (int i = 0; i < particleConfiguration.ParticleCount; i++)
            {
                unitCircleTrajectory = Random.insideUnitCircle * Random.Range(particleConfiguration.ParticleMinSpeed, particleConfiguration.ParticleMaxSpeed);
                velocity.x = unitCircleTrajectory.x;
                velocity.y = unitCircleTrajectory.y;
                velocity.z = Random.Range(particleConfiguration.ParticleMinZVelocity, particleConfiguration.ParticleMaxZVelocity) * (-1);
                velocity = rotation * velocity;
                
                // Set EmitParams for this particle
                emitParams.position = hitPoint + (hitNormal * 0.02f); // Z-fighting offset
                emitParams.velocity = velocity;
                emitParams.startSize = Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize);
                emitParams.startLifetime = Random.Range(particleConfiguration.ParticleMinLiveTime, particleConfiguration.ParticleMaxLiveTime);
                emitParams.startColor = particleConfiguration.ParticleColor;
                
                // DEBUG: Log first particle
                if (i == 0)
                {
                    Debug.Log($"[EMIT] Emitter: {particleConfiguration.ParticleEmitter.name}, Count: {particleConfiguration.ParticleCount}, Size: {emitParams.startSize}, Color: {emitParams.startColor}, Pos: {emitParams.position}");
                }
                
                // Emit single particle with params
                particleConfiguration.ParticleEmitter.Emit(emitParams, 1);
            }
            
            // DEBUG: Check particle count after emission
            Debug.Log($"[EMIT] After emission - ParticleSystem.particleCount: {particleConfiguration.ParticleEmitter.particleCount}, isPlaying: {particleConfiguration.ParticleEmitter.isPlaying}");
        }
    }

    public static void HitMaterialRotatingParticles(Vector3 hitPoint, Vector3 hitNormal, ParticleConfiguration particleConfiguration)
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
                velocity.z = Random.Range(particleConfiguration.ParticleMinZVelocity, particleConfiguration.ParticleMaxZVelocity) * (-1);
                velocity = rotation * velocity;
                var emitParams = new ParticleSystem.EmitParams();
                emitParams.position = hitPoint + (hitNormal * 0.02f); // Offset to prevent Z-fighting
                emitParams.velocity = velocity;
                emitParams.startSize = Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize);
                emitParams.startLifetime = Random.Range(particleConfiguration.ParticleMinLiveTime, particleConfiguration.ParticleMaxLiveTime);
                emitParams.startColor = particleConfiguration.ParticleColor;
                emitParams.rotation = Random.Range(0f, 360f);
                particleConfiguration.ParticleEmitter.Emit(emitParams, 1);
            }
        }
    }

    public static void HitMateriaHalfSphericParticles(Vector3 hitPoint, Vector3 hitNormal, ParticleConfiguration particleConfiguration)
    {
        if (particleConfiguration.ParticleEmitter != null)
        {
            Vector3 velocity = Vector3.zero;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.back, hitNormal);
            var emitParams = new ParticleSystem.EmitParams();

            for (int i = 0; i < particleConfiguration.ParticleCount; i++)
            {
                velocity = Random.insideUnitSphere * Random.Range(particleConfiguration.ParticleMinSpeed, particleConfiguration.ParticleMaxSpeed);
                if (velocity.z > 0) velocity.z = velocity.z * (-1);
                velocity = rotation * velocity;
                
                emitParams.position = hitPoint;
                emitParams.velocity = velocity;
                emitParams.startSize = Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize);
                emitParams.startLifetime = Random.Range(particleConfiguration.ParticleMinLiveTime, particleConfiguration.ParticleMaxLiveTime);
                emitParams.startColor = particleConfiguration.ParticleColor;
                particleConfiguration.ParticleEmitter.Emit(emitParams, 1);
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
                particleConfiguration.ParticleEmitter.Emit(hitPoint, velocity, Random.Range(particleConfiguration.ParticleMinSize, particleConfiguration.ParticleMaxSize), Random.Range(particleConfiguration.ParticleMinLiveTime, particleConfiguration.ParticleMaxLiveTime), particleConfiguration.ParticleColor);
            }
        }
    }
}