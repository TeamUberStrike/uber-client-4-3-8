using Cmune.Realtime.Common;
using UnityEngine;
using Cmune.Util;

[RequireComponent(typeof(BoxCollider))]
public class WaterGate : SecretDoor
{
    void Awake()
    {
        _state = DoorState.Closed;

        foreach (DoorElement e in _elements)
        {
            e.ClosedPosition = e.Element.transform.localPosition;
        }

        _doorID = transform.position.GetHashCode();
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
        switch (_state)
        {
            case DoorState.Closed: _state = DoorState.Opening; _currentTime = 0; break;
            case DoorState.Closing: _state = DoorState.Opening; _currentTime = _maxTime - _currentTime; break;
            case DoorState.Open: _timeToClose = Time.time + 2; break;
            case DoorState.Opening: break;
        }

        if (audio) audio.Play();
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

    void Update()
    {
        #region Old

        //if (_state == DoorState.Opening)
        //{
        //    _currentTime += Time.deltaTime;
        //    transform.localPosition = Vector3.Lerp(_closedPosition, _openPosition, _currentTime / _maxTime);
        //    if (_currentTime >= _maxTime)
        //    {
        //        _state = DoorState.Open;
        //        _timeToClose = Time.time + 2;
        //        if (audio) audio.Stop();
        //    }
        //}
        //else if (_state == DoorState.Open)
        //{
        //    if (_timeToClose < Time.time)
        //    {
        //        _state = DoorState.Closing;
        //        _currentTime = 0;

        //        if (audio) audio.Play();
        //    }
        //}
        //else if (_state == DoorState.Closing)
        //{
        //    _currentTime += Time.deltaTime;
        //    transform.localPosition = Vector3.Lerp(_openPosition, _closedPosition, _currentTime / _maxTime);

        //    if (_currentTime >= _maxTime)
        //    {
        //        _state = DoorState.Closed;
        //        if (audio) audio.Stop();
        //    }
        //}

        #endregion

                if (_state == DoorState.Opening)
        {
            _currentTime += Time.deltaTime;
            foreach (DoorElement e in _elements)
            {
                e.Element.transform.localPosition = Vector3.Lerp(e.ClosedPosition, e.OpenPosition, _currentTime / _maxTime);
            }

            //change state to open
            if (_currentTime >= _maxTime)
            {
                _state = DoorState.Open;
                _timeToClose = Time.time + 2;
                if (audio) audio.Stop();
            }
        }
        else if (_state == DoorState.Open)
        {
            //change state to closing
            if (_timeToClose < Time.time)
            {
                _state = DoorState.Closing;
                _currentTime = 0;

                if (audio) audio.Play();
            }
        }
        else if (_state == DoorState.Closing)
        {
            _currentTime += Time.deltaTime;
            foreach (DoorElement e in _elements)
            {
                e.Element.transform.localPosition = Vector3.Lerp(e.OpenPosition, e.ClosedPosition, _currentTime / _maxTime);
            }

            //change state to closed
            if (_currentTime >= _maxTime)
            {
                _state = DoorState.Closed;
                if (audio) audio.Stop();
            }
        }

    }

    [SerializeField]
    private float _maxTime = 1;

    //private Vector3 _closedPosition;
    //[SerializeField]
    //private Vector3 _openPosition;

    [SerializeField]
    private DoorElement[] _elements;

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

    private int _doorID;

    public int DoorID
    {
        get { return _doorID; }
    }

    [System.Serializable]
    public class DoorElement
    {
        [HideInInspector]
        public Vector3 ClosedPosition;
        [HideInInspector]
        public Quaternion ClosedRotation;

        public GameObject Element;
        public Vector3 OpenPosition;
    }
}
