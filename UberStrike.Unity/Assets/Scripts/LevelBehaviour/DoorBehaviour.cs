using Cmune.Util;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorBehaviour : MonoBehaviour
{
    // Use this for initialization
    void Awake()
    {
        //if (collider)
        //    collider.isTrigger = true;

        foreach (DoorElement e in _elements)
        {
            e.Element.SetDoorLogic(this);

            e.ClosedPosition = e.Element.Transform.localPosition;
        }

        _doorID = transform.position.GetHashCode();
    }

    void OnEnable()
    {
        CmuneEventHandler.AddListener<DoorOpenedEvent>(OnDoorOpenedEvent);
    }

    void OnDisable()
    {
        CmuneEventHandler.RemoveListener<DoorOpenedEvent>(OnDoorOpenedEvent);
    }

    private void OnDoorOpenedEvent(DoorOpenedEvent ev)
    {
        if (DoorID == ev.DoorID)
        {
            OpenDoor();
        }
    }

    // Update is called once per frame
    void OnTriggerEnter(Collider c)
    {
        if (c.tag == "Player")
        {
            Open();
        }
    }

    // Update is called once per frame
    void OnTriggerStay(Collider c)
    {
        if (c.tag == "Player")
        {
            //just keep the door open
            _timeToClose = Time.time + 2;
        }
    }

    private void OpenDoor()
    {
        switch (_state)
        {
            case DoorState.Closed: _state = DoorState.Opening; _currentTime = 0; if (GetComponent<AudioSource>()) GetComponent<AudioSource>().Play(); break;
            case DoorState.Closing: _state = DoorState.Opening; _currentTime = _maxTime - _currentTime; break;
            case DoorState.Open: _timeToClose = Time.time + 2; break;
            case DoorState.Opening: break;
        }
    }

    public void Open()
    {
        if (GameState.HasCurrentGame)
        {
            GameState.CurrentGame.OpenDoor(DoorID);
        }

        OpenDoor();
    }

    public void Close()
    {
        _state = DoorState.Closing;
        _currentTime = 0;

        if (GetComponent<AudioSource>()) GetComponent<AudioSource>().Play();
    }

    void Update()
    {
        float openTime = _maxTime;

        if (_maxTime == 0) openTime = 1;

        if (_state == DoorState.Opening)
        {
            _currentTime += Time.deltaTime;

            foreach (DoorElement e in _elements)
            {
                e.Element.Transform.localPosition = Vector3.Lerp(e.ClosedPosition, e.OpenPosition, _currentTime / openTime);
            }

            //change state to open
            if (_currentTime >= openTime)
            {
                _state = DoorState.Open;
                _timeToClose = Time.time + 2;

                if (GetComponent<AudioSource>()) GetComponent<AudioSource>().Stop();
            }
        }
        else if (_state == DoorState.Open)
        {
            //change state to closing
            if (_maxTime != 0 && _timeToClose < Time.time)
            {
                _state = DoorState.Closing;
                _currentTime = 0;

                if (GetComponent<AudioSource>()) GetComponent<AudioSource>().Play();
            }
        }
        else if (_state == DoorState.Closing)
        {
            _currentTime += Time.deltaTime;

            foreach (DoorElement e in _elements)
            {
                e.Element.Transform.localPosition = Vector3.Lerp(e.OpenPosition, e.ClosedPosition, _currentTime / openTime);
            }

            //change state to closed
            if (_currentTime >= openTime)
            {
                _state = DoorState.Closed;
                if (GetComponent<AudioSource>()) GetComponent<AudioSource>().Stop();
            }
        }
    }

    [SerializeField]
    private DoorElement[] _elements;
    [SerializeField]
    private float _maxTime = 1;

    private DoorState _state;
    private float _currentTime = 0;
    private float _timeToClose = 0;

    private enum DoorState
    {
        Closed = 0,
        Opening,
        Open,
        Closing,
    }

    [System.Serializable]
    public class DoorElement
    {
        [HideInInspector]
        public Vector3 ClosedPosition;
        [HideInInspector]
        public Quaternion ClosedRotation;

        public DoorTrigger Element;
        public Vector3 OpenPosition;
    }

    private int _doorID;

    public int DoorID
    {
        get { return _doorID; }
    }
}

public class DoorOpenedEvent
{
    public DoorOpenedEvent(int doorID)
    {
        DoorID = doorID;
    }

    public int DoorID { get { return _doorID; } protected set { _doorID = value; } }

    private int _doorID = 0;
}