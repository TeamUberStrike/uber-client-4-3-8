
using UnityEngine;
using System.Collections.Generic;
using Cmune.Util;

public static class MaterialUtil
{
    private static Dictionary<Material, MaterialCache> _cache = new Dictionary<Material, MaterialCache>();

    public static void SetFloat(Material m, string propertyName, float value)
    {
        if (m && m.HasProperty(propertyName))
        {
            //get the cache
            MaterialCache cache;
            if (!_cache.TryGetValue(m, out cache))
            {
                cache = new MaterialCache();
                _cache[m] = cache;
            }
            //write the original value
            if (!cache.Floats.ContainsKey(propertyName))
                cache.Floats[propertyName] = m.GetFloat(propertyName);

            m.SetFloat(propertyName, value);
        }
        else
        {
            CmuneDebug.LogError("Property<float> '{0}' not found in Material {1}", propertyName, m ? m.name : "NULL");
        }
    }

    public static void SetColor(Material m, string propertyName, Color value)
    {
        if (m && m.HasProperty(propertyName))
        {
            //get the cache
            MaterialCache cache;
            if (!_cache.TryGetValue(m, out cache))
            {
                cache = new MaterialCache();
                _cache[m] = cache;
            }
            //write the original value
            if (!cache.Colors.ContainsKey(propertyName))
                cache.Colors[propertyName] = m.GetColor(propertyName);

            m.SetColor(propertyName, value);
        }
        else
        {
            CmuneDebug.LogError("Property<Color> '{0}' not found in Material {1}", propertyName, m ? m.name : "NULL");
        }
    }

    public static void SetTextureOffset(Material m, string propertyName, Vector2 value)
    {
        if (m && m.HasProperty(propertyName))
        {
            //get the cache
            MaterialCache cache;
            if (!_cache.TryGetValue(m, out cache))
            {
                cache = new MaterialCache();
                _cache[m] = cache;
            }
            //write the original value
            if (!cache.TextureOffset.ContainsKey(propertyName))
                cache.TextureOffset[propertyName] = m.GetTextureOffset(propertyName);

            m.SetTextureOffset(propertyName, value);
        }
        else
        {
            CmuneDebug.LogError("Property<Vector2> '{0}' not found in Material {1}", propertyName, m ? m.name : "NULL");
        }
    }

    public static void SetTexture(Material m, string propertyName, Texture value)
    {
        if (m && m.HasProperty(propertyName))
        {
            //get the cache
            MaterialCache cache;
            if (!_cache.TryGetValue(m, out cache))
            {
                cache = new MaterialCache();
                _cache[m] = cache;
            }
            //write the original value
            if (!cache.Texture.ContainsKey(propertyName))
                cache.Texture[propertyName] = m.GetTexture(propertyName);

            m.SetTexture(propertyName, value);
        }
        else
        {
            CmuneDebug.LogError("Property<Texture> '{0}' not found in Material {1}", propertyName, m ? m.name : "NULL");
        }
    }

    public static void Reset(Material m)
    {
        MaterialCache cache;
        if (_cache.TryGetValue(m, out cache))
        {
            foreach (var v in cache.Colors)
            {
                m.SetColor(v.Key, v.Value);
            }
            foreach (var v in cache.Floats)
            {
                m.SetFloat(v.Key, v.Value);
            }
            foreach (var v in cache.TextureOffset)
            {
                m.SetTextureOffset(v.Key, v.Value);
            }
        }
    }

    private class MaterialCache
    {
        public Dictionary<string, Color> Colors = new Dictionary<string, Color>();
        public Dictionary<string, float> Floats = new Dictionary<string, float>();
        public Dictionary<string, Vector2> TextureOffset = new Dictionary<string, Vector2>();
        public Dictionary<string, Texture> Texture = new Dictionary<string, Texture>();
    }
}