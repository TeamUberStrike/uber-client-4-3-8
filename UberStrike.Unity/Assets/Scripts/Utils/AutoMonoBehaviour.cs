using System;
using UnityEngine;

public class AutoMonoBehaviour<T> : MonoBehaviour where T : class
{
    private static T _instance;
    private static GameObject _parent;
    private static bool _isRunning = true;

    private static GameObject Parent
    {
        get
        {
            if (_parent == null)
            {
                _parent = GameObject.Find("AutoMonoBehaviours");
                if (_parent == null)
                    _parent = new GameObject("AutoMonoBehaviours");
                //_parent.hideFlags = HideFlags.DontSave;// HideFlags.HideAndDontSave;
            }
            return _parent;
        }
    }

    private void OnApplicationQuit()
    {
        _isRunning = false;
    }

    private void Start()
    {
        //this line kicks in, if the script was dragged on to a gameobject at design time
        if (_instance == null) throw new Exception("The script " + typeof(T).Name + " is self instantiating and shouldn't be attached manually to a GameObject.");
    }

    public static T Instance
    {
        get
        {
            if (_instance == null && _isRunning)
            {
                if (--safetyCounter > 0)
                {
                    _instance = Parent.AddComponent(typeof(T)) as T;
                }
                else
                {
                    throw new Exception("Recursive calls to Constuctor of AutoMonoBehaviour! Check your " + typeof(T) + ".Awake() function for calls to " + typeof(T) + ".Instance");
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// This counter makes sure, that we don't get StackOverflow Exceptions due to resursive instatiations
    /// </summary>
    private static int safetyCounter = 100;
}