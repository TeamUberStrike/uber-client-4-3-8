using Cmune.Realtime.Common;
using UnityEngine;
using Cmune.Util;

[RequireComponent(typeof(BoxCollider))]
public class TempleTeleporter : SecretDoor
{
    private void Awake()
    {
        _audios = GetComponents<AudioSource>();

        _particles.emit = false;
        foreach(Renderer r in _visuals)
            r.enabled = false;

        _doorID = transform.position.GetHashCode();
    }

    private void OnEnable()
    {
        CmuneEventHandler.AddListener<DoorOpenedEvent>(OnDoorOpenedEvent);
    }

    private void OnDisable()
    {
        CmuneEventHandler.RemoveListener<DoorOpenedEvent>(OnDoorOpenedEvent);
    }

    private void Update()
    {
        if (_timeOut < Time.time)
        {
            foreach (AudioSource s in _audios) s.Stop();

            _particles.emit = false;
            foreach (Renderer r in _visuals)
                r.enabled = false;

            enabled = false;
        }
    }

    private void OnTriggerEnter(Collider c)
    {
        if (c.tag == "Player" && _timeOut > Time.time)
        {
            _timeOut = 0;

            GameState.LocalPlayer.SpawnPlayerAt(_spawnpoint.position, _spawnpoint.rotation);
        }
    }

    private void OnDoorOpenedEvent(DoorOpenedEvent ev)
    {
        if (DoorID == ev.DoorID)
        {
            OpenDoor();
        }
    }

    public override void Open()
    {
        if (GameState.HasCurrentGame)
        {
            GameState.CurrentGame.OpenDoor(DoorID);
        }

        OpenDoor();
    }

    private void OpenDoor()
    {
        enabled = true;

        _particles.emit = true;
        foreach (Renderer r in _visuals)
            r.enabled = true;

        _timeOut = Time.time + _activationTime;

        foreach (AudioSource s in _audios) s.Play();
    }

    public int DoorID
    {
        get { return _doorID; }
    }

    #region Fields
    [SerializeField]
    private float _activationTime = 15;
    [SerializeField]
    private Renderer[] _visuals;
    [SerializeField]
    private Transform _spawnpoint;
    [SerializeField]
    private ParticleSystem _particles;

    private int _doorID;
    private float _timeOut = 0;
    private AudioSource[] _audios;
    #endregion
}
