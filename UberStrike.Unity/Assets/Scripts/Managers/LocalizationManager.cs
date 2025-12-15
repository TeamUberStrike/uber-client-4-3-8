using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.Core.Types;
using UberStrike.Helper;
using UnityEngine;

public static class LocalizationHelper
{
    public static string GetLocaleString(LocaleType lang)
    {
        switch (lang)
        {
            case LocaleType.en_US: return LocaleIdentifier.EnUS;
            case LocaleType.ko_KR: return LocaleIdentifier.KoKR;
            default: throw new CmuneException("Call to GetLocalization failed because LocaleType {0} not handled!", lang);
        }
    }

    public static bool ValidateMemberName(string name, LocaleType locale = LocaleType.en_US)
    {
        switch (locale)
        {
            case LocaleType.ko_KR:
                return ValidationUtilities.IsValidMemberName(name, LocaleIdentifier.KoKR);
            default:
                return ValidationUtilities.IsValidMemberName(name);
        }
    }

    /// <summary>
    /// This function is called when the GUISkin is being initialized for a specific locale in Awake().
    /// You can e.g. replace the font of every GuiStyle with a custom localized font.
    /// This is not in use right now, but we keep this architecture to support localized fonts in the future.
    /// </summary>
    /// <param name="style"></param>
    /// <returns></returns>
    public static GUIStyle GetLocalizedStyle(GUIStyle style)
    {
        // If style is null or has a missing font, provide fallbacks
        if (style == null)
        {
            UnityEngine.Debug.LogWarning("GetLocalizedStyle received null style, creating fallback");
            return new GUIStyle();
        }

        // Clone the style to avoid modifying the original
        GUIStyle localizedStyle = new GUIStyle(style);

        // Check if the font exists and provide fallback if missing
        if (localizedStyle.font != null)
        {
            // Check if the font name suggests a missing custom font
            string fontName = localizedStyle.font.name;
            bool isCustomFont = fontName.Contains("InterparkGothic") || 
                               fontName.Contains("Interpark") || 
                               fontName.Contains("interparkbold") ||
                               fontName.Contains("interparkmed") ||
                               string.IsNullOrEmpty(fontName) ||
                               fontName.Equals("Missing");
                               
            if (isCustomFont)
            {
                UnityEngine.Debug.LogWarning($"Custom font '{fontName}' detected, replacing with system font for compatibility");
                
                // Use LegacyRuntime.ttf as primary fallback
                Font fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (fallbackFont == null)
                {
                    UnityEngine.Debug.LogWarning("LegacyRuntime.ttf not available, falling back to Arial.ttf");
                    fallbackFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                
                if (fallbackFont != null)
                {
                    localizedStyle.font = fallbackFont;
                }
                else
                {
                    // Use Unity's default font as last resort
                    localizedStyle.font = null;
                }
            }
        }

        // Handle case where font is missing entirely
        if (localizedStyle.font == null)
        {
            // Try to load Arial as fallback, or use Unity's default
            Font fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fallbackFont == null)
            {
                UnityEngine.Debug.LogWarning("LegacyRuntime.ttf not available, falling back to Arial.ttf");
                fallbackFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            
            if (fallbackFont != null)
            {
                localizedStyle.font = fallbackFont;
            }
        }

        return localizedStyle;
    }
}