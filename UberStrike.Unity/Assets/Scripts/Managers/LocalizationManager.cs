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
        //instead of returning the font we could modify it here.
        //for now just keep everything as it is
        return style;
    }
}