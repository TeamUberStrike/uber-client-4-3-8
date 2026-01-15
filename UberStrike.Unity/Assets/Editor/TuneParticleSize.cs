using UnityEngine;
using UnityEditor;

public class TuneParticleSize : MonoBehaviour
{
    [MenuItem("UberStrike/Tune Particle Size")]
    public static void Tune()
    {
        var pec = FindObjectOfType<ParticleEffectController>();
        if (pec == null)
        {
            Debug.LogError("Could not find ParticleEffectController.");
            return;
        }

        // 1. Tune SpawnParticles (Pickup Effect)
        TuneSystem(pec.transform, "SpawnParticles", ps => {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // Fix "Stuck in screen"
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f); // Random small size
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1.0f); // Random speed
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
            main.loop = false;
            
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere; // Emit in all directions
            shape.radius = 0.5f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-1f, 1f);
            vel.y = new ParticleSystem.MinMaxCurve(-1f, 1f);
            vel.z = new ParticleSystem.MinMaxCurve(-1f, 1f);

            Debug.Log($"[TUNED] 'SpawnParticles' -> World Space, Sphere Shape, Randomized.");
        });

        // 2. Tune "Floating Box" Candidates
        var allSystems = pec.GetComponentsInChildren<ParticleSystem>(true);
        foreach(var ps in allSystems)
        {
            var main = ps.main;
            main.playOnAwake = false; // Fix "Floating Boxes at 0,0,0"
            
            if(ps.name.Contains("Explosion") || ps.name.Contains("Splash") || ps.name.Contains("Water"))
            {
               main.loop = false;
               if(main.duration > 2f) main.duration = 1f;
            }
             
            // Resize if huge
            if(main.startSize.constant > 0.5f) {
                 main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            }
        }
        
        Debug.Log("[TUNED] Applied fixes to all particles (WorldSpace, NoAwake, resized).");
    }

    private static void TuneSystem(Transform root, string name, System.Action<ParticleSystem> action)
    {
        Transform t = root.Find(name);
        if (t == null)
        {
             foreach(Transform child in root) if(child.name.ToLower().Contains(name.ToLower())) { t = child; break; }
        }

        if (t != null)
        {
            var ps = t.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                action(ps);
                EditorUtility.SetDirty(ps);
            }
        }
    }
}
