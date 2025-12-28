using System;
using UnityEngine;
using UberStrike.Helper;

public static class StringUtils
{
    public static T ParseValue<T>(string value)
    {
        Type type = typeof(T);
        T returnValue = default(T);

        try
        {
            if (type.IsEnum)
            {
                returnValue = (T)Enum.Parse(type, value);
            }
            else if (type == typeof(int))
            {
                returnValue = (T)(object)int.Parse(value);
            }
            else if (type == typeof(string))
            {
                returnValue = (T)(object)value;
            }
            else if (type == typeof(DateTime))
            {
                returnValue = (T)(object)DateTime.Parse(TextUtilities.Base64Decode(value));
            }
            else
            {
                Debug.LogError("ParseValue couldn't find a conversion of value '" + value + "' into type '" + type.Name + "'");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("ParseValue failed converting value '" + value + "' into type '" + type.Name + "': " + e.Message);
        }

        return returnValue;
    }
}