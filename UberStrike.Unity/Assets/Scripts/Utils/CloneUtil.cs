using System;
using Cmune.Util;

public static class CloneUtil
{
    public static T Clone<T>(T instance) where T : class
    {
        Type type = instance.GetType();
        var cstr = type.GetConstructor(new Type[0]);
        if (cstr != null)
        {
            var clone = cstr.Invoke(new object[0]) as T;

            CopyAllFields(clone, instance);

            return clone;
        }
        else
        {
            return default(T);
        }
    }

    public static void CopyAllFields<T>(T destination, T source) where T : class
    {
        var destType = destination.GetType();
        var sourceFields = ReflectionHelper.GetAllFields(source.GetType(), true);
        var destFields = ReflectionHelper.GetAllFields(destType, true);
        
        foreach (var sourceField in sourceFields)
        {
            // Find matching field in destination type
            System.Reflection.FieldInfo destField = null;
            foreach (var df in destFields)
            {
                if (df.Name == sourceField.Name && df.FieldType == sourceField.FieldType)
                {
                    destField = df;
                    break;
                }
            }
            
            if (destField != null)
            {
                try
                {
                    destField.SetValue(destination, sourceField.GetValue(source));
                }
                catch (System.ArgumentException ex)
                {
                    // Log the field mismatch but continue with other fields
                    CmuneDebug.LogWarning("Field copy failed for " + sourceField.Name + ": " + ex.Message);
                }
            }
        }
    }
}