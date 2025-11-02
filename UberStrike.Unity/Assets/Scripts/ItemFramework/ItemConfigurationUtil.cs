
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
        CloneUtil.CopyAllFields(config, item);

        //CmuneDebug.LogError("Before CopyProperties for loop");
        //customize fields
        if (item.ID == 1234)
        {
            CmuneDebug.LogError("CopyProperties called for item ID 1234");
        }
        else
        {
            CmuneDebug.LogError("CopyProperties called for item ID " + item.ID);
        }
        foreach (var p in ReflectionHelper.GetAllFields(config.GetType(), true))
        {
            //CmuneDebug.LogError("Before GetCustomPropertyName");
            string name = GetCustomPropertyName(p);
            //CmuneDebug.LogError("Custom property name: " + name);

            if (!string.IsNullOrEmpty(name) && item.CustomProperties != null && item.CustomProperties.ContainsKey(name))
            {
                CmuneDebug.LogError("inside !IsNullOrEmpty ");
                //configure using custom property
                p.SetValue(config, Convert(item.CustomProperties[name], p.FieldType));
            }
        }
        //CmuneDebug.LogError("After CopyProperties for loop");
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