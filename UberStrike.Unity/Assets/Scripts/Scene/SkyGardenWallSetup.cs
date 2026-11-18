using System.Collections.Generic;
using UnityEngine;

// Revolving walls are static colliders and never push the CharacterController, so push the player out along the wall's motion.
public static class SkyGardenWallSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        var host = new GameObject("SkyGardenWallController");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<SkyGardenWallController>();
    }
}

public class SkyGardenWallController : MonoBehaviour
{
    private const int SkyGardenMapId = 7;
    private const float PushStep = 0.05f;
    private const float MaxPush = 2f;
    private static readonly string[] WallNames = { "BigWall", "SmallWall" };

    private class Wall
    {
        public Transform Transform;
        public HashSet<Collider> Colliders;
        public float LastYaw;
    }

    private readonly List<Wall> _walls = new List<Wall>();
    private MapConfiguration _space;
    private readonly Collider[] _overlaps = new Collider[16];

    private void LateUpdate()
    {
        if (!GameState.Exists || !GameState.HasCurrentSpace || GameState.CurrentSpace == null || GameState.CurrentSpace.MapId != SkyGardenMapId)
        {
            _walls.Clear();
            _space = null;
            return;
        }

        if (_space != GameState.CurrentSpace) FindWalls(GameState.CurrentSpace);
        if (_walls.Count == 0 || !GameState.HasCurrentPlayer || GameState.LocalPlayer == null) return;

        var controller = GameState.LocalPlayer.GetComponent<CharacterController>();
        foreach (var wall in _walls)
        {
            if (wall.Transform == null) continue;
            float yaw = wall.Transform.eulerAngles.y;
            float delta = Mathf.DeltaAngle(wall.LastYaw, yaw);
            wall.LastYaw = yaw;
            if (controller == null || !controller.enabled || Mathf.Abs(delta) < 0.0001f) continue;
            if (!OverlapsWall(controller, controller.transform.position, wall)) continue;

            Vector3 radial = controller.transform.position + controller.center - wall.Transform.position;
            radial.y = 0;
            Vector3 surfaceMove = Quaternion.AngleAxis(delta, Vector3.up) * radial - radial;
            if (surfaceMove.sqrMagnitude < 0.000001f) continue;
            Vector3 dir = surfaceMove.normalized;

            for (float d = PushStep; d <= MaxPush; d += PushStep)
            {
                Vector3 target = controller.transform.position + dir * d;
                if (OverlapsWall(controller, target, wall)) continue;
                if (OverlapsAnything(controller, target)) break;
                controller.enabled = false;
                controller.transform.position = target;
                controller.enabled = true;
                break;
            }
        }
    }

    private void FindWalls(MapConfiguration space)
    {
        _walls.Clear();
        _space = space;
        foreach (var t in space.GetComponentsInChildren<Transform>(true))
        {
            if (System.Array.IndexOf(WallNames, t.name) < 0) continue;
            var colliders = new HashSet<Collider>();
            foreach (var c in t.GetComponentsInChildren<Collider>(true))
                if (!c.isTrigger) colliders.Add(c);
            _walls.Add(new Wall { Transform = t, Colliders = colliders, LastYaw = t.eulerAngles.y });
        }
    }

    private bool OverlapsWall(CharacterController controller, Vector3 position, Wall wall)
    {
        int n = OverlapAt(controller, position);
        for (int i = 0; i < n; i++)
            if (wall.Colliders.Contains(_overlaps[i])) return true;
        return false;
    }

    private bool OverlapsAnything(CharacterController controller, Vector3 position)
    {
        int n = OverlapAt(controller, position);
        for (int i = 0; i < n; i++)
            if (_overlaps[i] != controller && !_overlaps[i].transform.IsChildOf(controller.transform)) return true;
        return false;
    }

    private int OverlapAt(CharacterController controller, Vector3 position)
    {
        Vector3 center = position + controller.center;
        float radius = controller.radius - 0.02f;
        float half = Mathf.Max(0f, controller.height * 0.5f - controller.radius);
        return Physics.OverlapCapsuleNonAlloc(center + Vector3.up * half, center - Vector3.up * half, radius, _overlaps, UberstrikeLayerMasks.CrouchMask, QueryTriggerInteraction.Ignore);
    }
}
