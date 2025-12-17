
using System;
using System.Globalization;
using System.Reflection;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.Core.Models.Views;

public static class ItemConfigurationUtil
{
    public static void CopyProperties<T>(T config, BaseUberStrikeItemView item) where T : BaseUberStrikeItemView
    {
        try
        {
            CloneUtil.CopyAllFields(config, item);
        }
        catch (System.Exception ex)
        {
            CmuneDebug.LogError("Failed to copy fields between {0} and {1}: {2}", config.GetType().Name, item.GetType().Name, ex.Message);
            throw;
        }

        //customize fields
        foreach (var p in ReflectionHelper.GetAllFields(config.GetType(), true))
        {
            string name = GetCustomPropertyName(p);

            if (!string.IsNullOrEmpty(name) && item.CustomProperties != null && item.CustomProperties.ContainsKey(name))
            {
                try
                {
                    //configure using custom property
                    p.SetValue(config, Convert(item.CustomProperties[name], p.FieldType));
                }
                catch (System.Exception ex)
                {
                    CmuneDebug.LogError("Failed to set custom property {0} on {1}: {2}", name, config.GetType().Name, ex.Message);
                }
            }
        }
    }

    private static string GetCustomPropertyName(FieldInfo info)
    {
        object[] atts = info.GetCustomAttributes(typeof(CustomPropertyAttribute), true);
        return atts.Length > 0 ? ((CustomPropertyAttribute)atts[0]).Name : string.Empty;
    }

    private static object Convert(string value, Type type)
    {
        if (type == typeof(string))
            return value;
        else if (type.IsEnum || type == typeof(int))
            return int.Parse(value, CultureInfo.InvariantCulture);
        else if (type == typeof(float))
            return float.Parse(value, CultureInfo.InvariantCulture);
        else if (type == typeof(bool))
            return bool.Parse(value);
        else
            throw new System.NotSupportedException("ConfigurableItem has unsupported property of type: " + type);
    }
}