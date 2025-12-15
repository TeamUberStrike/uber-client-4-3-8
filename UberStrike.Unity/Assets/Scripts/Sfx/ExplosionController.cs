using UnityEngine;

/// <summary>
/// Explosion controller for Unity 6 compatibility
/// </summary>
public class ExplosionController
{
    public void EmitBlast(Vector3 hitPoint, Vector3 hitNormal, ExplosionBaseParameters parameters)
    {
        Vector3 velocity = Vector3.zero;

        float size;
        float energy;
        if (parameters.ParticleEmitter != null)
        {
            for (int i = 0; i < parameters.ParticleCount; i++)
            {
                size = Random.Range(parameters.MinSize, parameters.MaxSize);
                energy = Random.Range(parameters.MinLifeTime, parameters.MaxLifeTime);
                parameters.ParticleEmitter.Emit(hitPoint, velocity, size, energy, Color.red);
            }
        }
    }

    public void EmitDust(Vector3 hitPoint, Vector3 hitNormal, ExplosionDustParameters parameters)
    {
        Vector3 velocity = Vector3.zero;
        float size;
        float energy;
        if (parameters.ParticleEmitter != null)
        {
            for (int i = 0; i < parameters.ParticleCount; i++)
            {
                velocity = Random.insideUnitSphere * 0.2f;
                hitPoint = hitPoint + Random.insideUnitSphere * Random.Range(parameters.MinStartPositionSize, parameters.MinStartPositionSize);
                size = Random.Range(parameters.MinSize, parameters.MaxSize);
                energy = Random.Range(parameters.MinLifeTime, parameters.MaxLifeTime);
                parameters.ParticleEmitter.Emit(hitPoint, velocity, size, energy, Color.red);
            }
        }
    }

    public void EmitRing(Vector3 hitPoint, Vector3 hitNormal, ExplosionRingParameters parameters)
    {
        Vector3 velocity = Vector3.zero;
        float size;
        float energy;
        size = parameters.StartSize;
        energy = Random.Range(parameters.MinLifeTime, parameters.MaxLifeTime);
        if (parameters.ParticleEmitter != null)
        {
            parameters.ParticleEmitter.Emit(hitPoint, velocity, size, energy, Color.red);
        }
    }

    public void EmitSmoke(Vector3 hitPoint, Vector3 hitNormal, ExplosionBaseParameters parameters)
    {
        Vector3 velocity = Vector3.zero;
        float size;
        float energy;

        //Quaternion rotation = new Quaternion();
        //rotation = Quaternion.FromToRotation(Vector3.back, hitNormal);

        if (parameters.ParticleEmitter != null)
        {
            for (int i = 0; i < parameters.ParticleCount; i++)
            {
                size = Random.Range(parameters.MinSize, parameters.MaxSize);
                energy = Random.Range(parameters.MinLifeTime, parameters.MaxLifeTime);
                velocity = Random.insideUnitSphere * 0.3f;
                //velocity = rotation * velocity;
                parameters.ParticleEmitter.Emit(hitPoint, velocity, size, energy, Color.red);
            }
        }
    }

    public void EmitSpark(Vector3 hitPoint, Vector3 hitNormal, ExplosionSphericParameters parameters)
    {
        Vector3 velocity = Vector3.zero;
        float size;
        float energy;
        if (parameters.ParticleEmitter != null)
        {
            for (int i = 0; i < parameters.ParticleCount; i++)
            {
                size = Random.Range(parameters.MinSize, parameters.MaxSize);
                energy = Random.Range(parameters.MinLifeTime, parameters.MaxLifeTime);
                velocity = Random.insideUnitSphere * parameters.Speed;
                parameters.ParticleEmitter.Emit(hitPoint, velocity, size, energy, Color.red);
            }
        }
    }

    public void EmitTrail(Vector3 hitPoint, Vector3 hitNormal, ExplosionSphericParameters parameters)
    {
        Vector3 velocity = Vector3.zero;
        float size;
        float energy;
        if (parameters.ParticleEmitter != null)
        {
            for (int i = 0; i < parameters.ParticleCount; i++)
            {
                size = Random.Range(parameters.MinSize, parameters.MaxSize);
                energy = Random.Range(parameters.MinLifeTime, parameters.MaxLifeTime);
                velocity = Random.insideUnitSphere * parameters.Speed;
                parameters.ParticleEmitter.Emit(hitPoint, velocity, size, energy, Color.red);
            }
        }
    }
}