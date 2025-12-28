using System;
using UnityEngine;

/// <summary>
/// This class is used to inject delegates for the basic Unity runtime callback
/// </summary>
public class UnityRuntime : AutoMonoBehaviour<UnityRuntime>
{
    public event Action OnGui;
    public event Action OnUpdate;
    public event Action OnFixedUpdate;

    void FixedUpdate()
    {
        if (OnFixedUpdate != null)
            OnFixedUpdate();
    }

    void Update()
    {
        if (OnUpdate != null)
            OnUpdate();
    }

    void OnGUI()
    {
        if (OnGui != null)
            OnGui();
    }
}