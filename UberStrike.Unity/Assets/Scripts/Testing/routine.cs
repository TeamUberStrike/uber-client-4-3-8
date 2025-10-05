using UnityEngine;
using System.Collections;
using System;

class routine : MonoBehaviour
{
    void Start()
    {
        try
        {
            StartCoroutine(Test(e => Debug.LogWarning("Catch1: " + e.Message)));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Catch2: " + e.Message);
        }
        Debug.Log("run");
    }

    IEnumerator Test(Action<Exception> exception)
    {
        yield return new WaitForSeconds(1);
        try
        {
            throw new Exception("hello");
        }
        catch (Exception e)
        {
            exception(e);
        }
    }
}