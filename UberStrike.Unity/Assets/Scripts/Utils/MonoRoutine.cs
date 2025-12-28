using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoRoutine : AutoMonoBehaviour<MonoRoutine>
{
    public delegate IEnumerator FunctionFloat(float f);
    public delegate IEnumerator FunctionInt(int i);
    public delegate IEnumerator FunctionVoid();

    private static readonly List<string> _runningRoutines = new List<string>();

    private static bool _isApplicationQuitting = false;

    private void OnApplicationQuit()
    {
        _isApplicationQuitting = true;
    }

    void Update()
    {
        if (OnUpdateEvent != null)
            OnUpdateEvent();
    }

    public static Coroutine Start(IEnumerator routine)
    {
        if (!_isApplicationQuitting)
            return Instance.StartCoroutine(routine);
        else
            return null;
    }

    private static Coroutine Start(IEnumerator routine, string code)
    {
        if (!_isApplicationQuitting)
            return Instance.StartCoroutine(Instance.StartSafeRoutine(routine, code));
        else
            return null;
    }

    private IEnumerator StartSafeRoutine(IEnumerator routine, string code)
    {
        if (!_runningRoutines.Contains(code))
        {
            _runningRoutines.Add(code);
            yield return Instance.StartCoroutine(routine);
            _runningRoutines.Remove(code);
        }
        else
        {
            Debug.LogWarning("Ignored multiple call to routine " + code);
        }
    }

    /// <summary>
    /// Ensures to run a single coroutine only 1 time until it ends, no matter how many calls are recieved
    /// </summary>
    /// <param name="run"></param>
    /// <param name="f"></param>
    /// <returns></returns>
    public static Coroutine Run(FunctionFloat run, float f)
    {
        string code = "Run " + run.Method.GetHashCode() + " " + run.Target.GetHashCode();
        return MonoRoutine.Start(run(f), code);
    }

    public static Coroutine Run(FunctionInt run, int i)
    {
        string code = "Run " + run.Method.GetHashCode() + " " + run.Target.GetHashCode();
        return MonoRoutine.Start(run(i), code);
    }

    public static Coroutine Run(FunctionVoid run)
    {
        string code = "Run " + run.Method.GetHashCode() + " " + run.Target.GetHashCode();
        return MonoRoutine.Start(run(), code);
    }

    public event Action OnUpdateEvent;
}