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
        foreach (var p in ReflectionHelper.GetAllFields(source.GetType(), true))
        {
            p.SetValue(destination, p.GetValue(source));
        }
    }
}