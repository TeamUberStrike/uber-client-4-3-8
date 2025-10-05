using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class PreemptiveCoroutineManager : Singleton<PreemptiveCoroutineManager>
{
    public delegate IEnumerator CoroutineFunction();

    public int IncrementId(CoroutineFunction func)
    {
        if (coroutineFuncIds.ContainsKey(func))
        {
            return ++coroutineFuncIds[func];
        }
        else
        {
            return ResetCoroutineId(func);
        }
    }

    public bool IsCurrent(CoroutineFunction func, int coroutineId)
    {
        if (coroutineFuncIds.ContainsKey(func))
        {
            return coroutineFuncIds[func] == coroutineId;
        }
        else
        {
            return false;
        }
    }

    public int ResetCoroutineId(CoroutineFunction func)
    {
        coroutineFuncIds[func] = 0;
        return 0;
    }

    private PreemptiveCoroutineManager()
    {
        coroutineFuncIds = new Dictionary<CoroutineFunction, int>();
    }

    private Dictionary<CoroutineFunction, int> coroutineFuncIds;
}

