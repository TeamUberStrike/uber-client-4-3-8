using System.Collections.Generic;
using UnityEngine;

// Sky Garden's revolving walls (BigWall, SmallWall) are turned by legacy Animation clips that every client
// starts on its own when the map loads, so players who joined at different times see the walls at different
// angles. Drive both clips from the synced server clock instead, so every client has the same wall angle.
// This also keeps projectile hits consistent: any client that sees a rocket hit a wall explodes it for all.
public static class SkyGardenWallSync
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        var host = new GameObject("SkyGardenWallSync");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<SkyGardenWallSyncController>();
    }
}

public class SkyGardenWallSyncController : MonoBehaviour
{
    private const int SkyGardenMapId = 7;
    private static readonly string[] WallNames = { "BigWall", "SmallWall" };

    private readonly List<Animation> _walls = new List<Animation>();
    private MapConfiguration _space;

    private void Update()
    {
        if (!GameState.Exists || !GameState.HasCurrentSpace || GameState.CurrentSpace == null || GameState.CurrentSpace.MapId != SkyGardenMapId)
        {
            _walls.Clear();
            _space = null;
            return;
        }

        if (_space != GameState.CurrentSpace) FindWalls(GameState.CurrentSpace);
        if (_walls.Count == 0) return;

        int now = ServerTimeMs();
        foreach (var anim in _walls)
        {
            if (anim == null || !anim.isActiveAndEnabled || anim.clip == null) continue;
            AnimationState state = anim[anim.clip.name];
            if (state == null) continue;

            int lengthMs = Mathf.Max(1, Mathf.RoundToInt(state.length * 1000f));
            if (!anim.isPlaying) anim.Play(anim.clip.name);
            state.speed = 0f;
            state.time = (now % lengthMs) / 1000f;
            anim.Sample();
        }
    }

    // Server milliseconds (int31), the same clock the match timer uses. Offline it is the local clock.
    public static int ServerTimeMs()
    {
        try
        {
            var client = GameConnectionManager.Client;
            if (client != null && client.PeerListener != null) return client.PeerListener.ServerTimeTicks & int.MaxValue;
        }
        catch (System.Exception)
        {
        }
        return (int)(Time.realtimeSinceStartup * 1000f) & int.MaxValue;
    }

    private void FindWalls(MapConfiguration space)
    {
        _walls.Clear();
        _space = space;
        foreach (var anim in space.GetComponentsInChildren<Animation>(true))
            if (System.Array.IndexOf(WallNames, anim.name) >= 0) _walls.Add(anim);
    }
}
