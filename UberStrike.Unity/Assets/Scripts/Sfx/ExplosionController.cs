using UnityEngine;

public class ExplosionController:MonoBehaviour
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
                ParticleEmissionSystem.EmitSafe(parameters.ParticleEmitter, hitPoint, velocity, size, energy, Color.white);
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
                ParticleEmissionSystem.EmitSafe(parameters.ParticleEmitter, hitPoint, velocity, size, energy, Color.white);
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
            ParticleEmissionSystem.EmitSafe(parameters.ParticleEmitter, hitPoint, velocity, size, energy, Color.white);
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
                ParticleEmissionSystem.EmitSafe(parameters.ParticleEmitter, hitPoint, velocity, size, energy, Color.white);
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
                ParticleEmissionSystem.EmitSafe(parameters.ParticleEmitter, hitPoint, velocity, size, energy, Color.white);
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
                ParticleEmissionSystem.EmitSafe(parameters.ParticleEmitter, hitPoint, velocity, size, energy, Color.white);
            }
        }
    }
}