using System.Collections;
using System.Collections.Generic;
using Cmune.Util;

public static class CoroutineManager
{
    private static int _routineId = 1;

    public delegate IEnumerator CoroutineFunction();

    public static Dictionary<CoroutineFunction, int> coroutineFuncIds = new Dictionary<CoroutineFunction, int>();

    public static void StartCoroutine(CoroutineFunction func, bool unique = true)
    {
        if (!unique || !IsRunning(func))
        {
            MonoRoutine.Start(func());
        }
        else
        {
            //CmuneDebug.LogWarning("StartCoroutine '" + func.Method.Name + "()' ignored because already running");
        }
    }

    public static int Begin(CoroutineFunction func)
    {
        coroutineFuncIds[func] = ++_routineId;
        
        return _routineId;
    }

    public static void End(CoroutineFunction func, int id)
    {
        if (coroutineFuncIds.ContainsKey(func) && coroutineFuncIds[func] == id)
            coroutineFuncIds.Remove(func);
    }

    public static bool IsRunning(CoroutineFunction func)
    {
        return coroutineFuncIds.ContainsKey(func);
    }

    public static bool IsCurrent(CoroutineFunction func, int coroutineId)
    {
        int routineId = 0;
        coroutineFuncIds.TryGetValue(func, out routineId);
        return routineId == coroutineId;
    }

    public static void StopCoroutine(CoroutineFunction func)
    {
        coroutineFuncIds.Remove(func);
    }
}