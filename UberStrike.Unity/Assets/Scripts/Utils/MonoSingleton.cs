using UnityEngine;
using System;

public class MonoSingleton<T> : MonoBehaviour where T : class
{
    private static UnityEngine.Object[] _sceneInstances;
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }
            else
            {
                _sceneInstances = GameObject.FindSceneObjectsOfType(typeof(T));

                if (_sceneInstances.Length == 0)
                {
                    string message = typeof(T).Name + " is not attached to a gameObject or the gameObject is not active.\nMake also sure to set 'Instance = this;' in your Awake() function!";
                    throw new NullReferenceException(message);
                }
                else if (_sceneInstances.Length > 1)
                {
                    string message = typeof(T).Name + " is attached to multiple GameObjects. Make sure to use it as a singleton only.";
                    throw new NullReferenceException(message);
                }

                _instance = _sceneInstances[0] as T;

                return _instance;
            }
        }
    }

    public static bool Exists
    {
        get
        {
            if (_sceneInstances == null)
            {
                _sceneInstances = GameObject.FindSceneObjectsOfType(typeof(T));
            }

            return _sceneInstances.Length > 0;
        }
    }

    private void OnApplicationQuit()
    {
        _instance = null;

        OnShutdown();
    }

    protected virtual void OnShutdown() { }
}