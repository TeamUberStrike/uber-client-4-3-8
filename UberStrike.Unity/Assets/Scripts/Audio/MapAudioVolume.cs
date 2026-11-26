using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World volume option (map part): scales every sound source placed in the current map (ambience, jump pad hum,
/// doors, water gates, ...). The volume set in the map is remembered per source and multiplied by the
/// option; sources an AudioFadeArea has taken over get the option from that area instead.
/// Avatars, weapons and projectiles that end up in the map scene are left alone.
/// </summary>
public static class MapAudioVolume
{
    private static readonly Dictionary<AudioSource, float> _original = new Dictionary<AudioSource, float>();
    private static MapConfiguration _map;

    /// <summary>Called when a map is switched on and when the option changes.</summary>
    public static void Apply(MapConfiguration map)
    {
        if (map == null) return;
        if (map != _map)
        {
            _original.Clear();
            _map = map;
        }

        HashSet<AudioSource> controlled = new HashSet<AudioSource>();
        AudioFadeArea[] fadeAreas = Object.FindObjectsOfType<AudioFadeArea>(true);
        foreach (AudioFadeArea area in fadeAreas) area.AddControlledSources(controlled);

        float factor = SfxManager.WorldAudioVolume;
        foreach (AudioSource source in Sources(map))
        {
            if (controlled.Contains(source)) continue;
            float original;
            if (!_original.TryGetValue(source, out original))
            {
                original = source.volume;
                _original[source] = original;
            }
            source.volume = original * factor;
        }

        foreach (AudioFadeArea area in fadeAreas) area.ApplyMapVolume();
    }

    /// <summary>The map's own sound sources: under the MapConfiguration and its static content.</summary>
    public static List<AudioSource> Sources(MapConfiguration map)
    {
        HashSet<AudioSource> found = new HashSet<AudioSource>(map.GetComponentsInChildren<AudioSource>(true));
        if (map.StaticContent != null) found.UnionWith(map.StaticContent.GetComponentsInChildren<AudioSource>(true));
        List<AudioSource> result = new List<AudioSource>();
        foreach (AudioSource source in found)
            if (source != null && IsMapSound(source)) result.Add(source);
        return result;
    }

    private static bool IsMapSound(AudioSource source)
    {
        return source.GetComponentInParent<CharacterConfig>(true) == null
            && source.GetComponentInParent<BaseWeaponDecorator>(true) == null
            && source.GetComponentInParent<Projectile>(true) == null;
    }
}
