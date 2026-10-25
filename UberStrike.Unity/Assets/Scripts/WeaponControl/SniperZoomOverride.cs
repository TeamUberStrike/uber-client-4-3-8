using System.Collections.Generic;

// prefab zoom clobbered at runtime. 4.7.1 values, ZoomInfo(default, min, max)
public static class SniperZoomOverride
{
    private static readonly Dictionary<int, ZoomInfo> _byId = new Dictionary<int, ZoomInfo>
    {
        { 1004, new ZoomInfo(2f, 2f, 4f) },   // Sniper Rifle
        { 1016, new ZoomInfo(2f, 2f, 4f) },   // Ordinator Rifle
        { 1017, new ZoomInfo(2f, 2f, 4f) },   // Deliverator
        { 1018, new ZoomInfo(2f, 2f, 8f) },   // Vanquisher
        { 1331, new ZoomInfo(2f, 2f, 8f) },   // Vanquisher Kongregate
        { 1147, new ZoomInfo(4f, 4f, 4f) },   // Particle Lance
        { 1239, new ZoomInfo(2f, 2f, 8f) },   // Nefarious Needler
        { 1244, new ZoomInfo(2f, 2f, 16f) },  // Dark Vanquisher
        { 1246, new ZoomInfo(2f, 2f, 8f) },   // Fusion Lance
        { 1300, new ZoomInfo(2f, 2f, 4f) },   // Snap Shot
        { 1301, new ZoomInfo(2f, 2f, 8f) },   // Vanquisher [Dragon]
        { 1316, new ZoomInfo(4f, 4f, 4f) },   // Particle Lance [Dragon]
        { 1355, new ZoomInfo(2f, 2f, 16f) },  // AWP
        { 1356, new ZoomInfo(2f, 2f, 16f) },  // AWP [Black]
        { 1357, new ZoomInfo(2f, 2f, 16f) },  // AWP [Camo]
        { 1358, new ZoomInfo(2f, 2f, 16f) },  // AWP [Tiger]
        { 9012, new ZoomInfo(2f, 2f, 4f) },   // Void Amethyst
    };

    // fresh copy per equip
    public static ZoomInfo Resolve(WeaponItemConfiguration config)
    {
        if (config != null && _byId.TryGetValue(config.ID, out var template))
            return new ZoomInfo(template.DefaultMultiplier, template.MinMultiplier, template.MaxMultiplier);
        return config != null ? config.ZoomInformation : null;
    }
}
