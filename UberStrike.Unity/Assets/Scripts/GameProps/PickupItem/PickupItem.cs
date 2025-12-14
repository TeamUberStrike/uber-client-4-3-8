using System.Collections;
using System.Collections.Generic;
using Cmune.Util;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PickupItem : MonoBehaviour
{
    #region Fields

    [SerializeField]
    protected int _respawnTime = 20;

    [SerializeField]
    private ParticleSystem _emitter;

    [SerializeField]
    protected Transform _pickupItem;

    protected MeshRenderer[] _renderers;

    private bool _isAvailable;
    public bool IsAvailable
    {
        get { return _isAvailable; }
        protected set
        {
            _isAvailable = value;
        }
    }

    private int _pickupID = 0;
    private Collider _collider;
    private static int _instanceCounter = 0;
    private static Dictionary<int, PickupItem> _instances = new Dictionary<int, PickupItem>();
    private static List<byte> _pickupRespawnDurations = new List<byte>();

    #endregion

    protected virtual void Awake()
    {
        _collider = GetComponent<Collider>();
        if (_pickupItem)
            _renderers = _pickupItem.GetComponentsInChildren<MeshRenderer>(true);
        else
            _renderers = new MeshRenderer[0];

        _collider.isTrigger = true;

        if (_emitter) 
        {
            var emission = _emitter.emission;
            emission.enabled = false;
        }

        gameObject.layer = (int)UberstrikeLayer.IgnoreRaycast;
    }

    void OnEnable()
    {
        IsAvailable = true;

        _pickupID = AddInstance(this);

        foreach (Renderer r in _renderers)
            r.enabled = true;

        CmuneEventHandler.AddListener<PickupItemEvent>(OnRemotePickupEvent);
    }

    void OnDisable()
    {
        CmuneEventHandler.RemoveListener<PickupItemEvent>(OnRemotePickupEvent);
    }

    private void OnRemotePickupEvent(PickupItemEvent ev)
    {
        if (PickupID == ev.PickupID)
        {
            SetItemAvailable(ev.ShowItem);

            if (!ev.ShowItem && IsAvailable)
            {
                OnRemotePickup();
            }
        }
    }

    protected virtual void OnRemotePickup() { }

    private void OnTriggerEnter(Collider c)
    {
        if (IsAvailable && c.tag == "Player")
        {
            if (GameState.HasCurrentPlayer && GameState.LocalCharacter.IsAlive)
            {
                if (OnPlayerPickup())
                {
                    SetItemAvailable(false);
                }
            }
        }
    }

    protected void PlayLocalPickupSound(SoundEffectType soundEffectType)
    {
        SfxManager.Play2dAudioClip(soundEffectType);
    }

    protected void PlayRemotePickupSound(SoundEffectType soundEffectType, Vector3 position)
    {
        SfxManager.Play3dAudioClip(soundEffectType, position);
    }

    protected IEnumerator StartHidingPickupForSeconds(int seconds)
    {
        IsAvailable = false;

        ParticleEffectController.ShowPickUpEffect(_pickupItem.position, 100);

        foreach (Renderer r in _renderers)
        {
            if (r != null)
            {
                r.enabled = false;
            }
        }

        if (seconds > 0)
        {
            // Respawn after n seconds
            yield return new WaitForSeconds(seconds);

            ParticleEffectController.ShowPickUpEffect(_pickupItem.position, 5);

            yield return new WaitForSeconds(1);

            foreach (Renderer r in _renderers)
                r.enabled = true;

            IsAvailable = true;
        }
        else
        {
            // If now respawn time is given we destroy the pickup item
            //disable Update and OnGUI calls
            enabled = false;

            yield return new WaitForSeconds(2);

            GameObject.Destroy(gameObject);
        }
    }

    public void SetItemAvailable(bool isVisible)
    {
        // on respawn
        if (isVisible)
        {
            ParticleEffectController.ShowPickUpEffect(_pickupItem.position, 5);
        }
        // on pickup
        else if (IsAvailable)
        {
            ParticleEffectController.ShowPickUpEffect(_pickupItem.position, 100);
        }

        foreach (Renderer r in _renderers)
        {
            if (r)
            {
                r.enabled = isVisible;
            }
        }

        IsAvailable = isVisible;
    }

    protected virtual bool OnPlayerPickup() { return true; }

    protected virtual bool CanPlayerPickup { get { return true; } }

    public int PickupID
    {
        get { return _pickupID; }
        set { _pickupID = value; }
    }

    public int RespawnTime
    {
        get { return _respawnTime; }
    }

    public static void ResetInstanceCounter()
    {
        _instanceCounter = 0;
        _instances.Clear();
        _pickupRespawnDurations.Clear();
    }

    public static int GetInstanceCounter()
    {
        return _instanceCounter;
    }

    public static List<byte> GetRespawnDurations()
    {
        return _pickupRespawnDurations;
    }

    private static int AddInstance(PickupItem i)
    {
        int index = _instanceCounter++;

        _instances[index] = i;
        _pickupRespawnDurations.Add((byte)i.RespawnTime);

        return index;
    }

    public static PickupItem GetInstance(int id)
    {
        PickupItem instance = null;

        _instances.TryGetValue(id, out instance);

        return instance;
    }
}

public class PickupItemEvent
{
    public PickupItemEvent(int pickupID, bool showItem)
    {
        ShowItem = showItem;
        PickupID = pickupID;
    }

    public int PickupID { get; private set; }
    public bool ShowItem { get; private set; }
}