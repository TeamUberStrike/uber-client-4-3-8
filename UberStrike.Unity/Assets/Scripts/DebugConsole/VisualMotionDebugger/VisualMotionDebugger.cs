//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;
//using System;
//using UberStrike.Realtime.Common;

//public class VisualMotionDebugger : MonoSingleton<VisualMotionDebugger>
//{
//    [SerializeField]
//    private GameObject _markerPrefabLocal;

//    [SerializeField]
//    private GameObject _markerPrefabGameFrame;

//    [SerializeField]
//    private Transform _markerLocalParent;

//    [SerializeField]
//    private Transform _markerGameFrameParent;

//    [SerializeField]
//    private Transform _playerLocalTransform;

//    private float _intervalLocal = 0.1f; // Seconds
//    private float _keepAliveLocal = 10.0f; // Seconds    
//    private float _intervalGameFrame = 0.1f; // Seconds
//    private float _keepAliveGameFrame = 10.0f; // Seconds

//    private float _currentLocalTime = 0.0f;
//    private float _currentGameFrameTime = 0.0f;

//    private float _startRecordTime = 0.0f;
//    private float _stopRecordTime = 0.0f;
//    private float _recordTimeLength = 0.0f;

//    private bool _recordActive = false;
//    private bool _recordLocal = true;
//    private bool _recordGameFrame = true;

//    public class MotionMarker
//    {
//        public string Name;
//        public float TimeStamp;
//        public GameObject MarkerPrefab;
//        public float SecondsRemaining;

//        public MotionMarker(float timeStamp, GameObject markerPrefab, float secondsRemainging)
//        {
//            TimeStamp = timeStamp;
//            MarkerPrefab = markerPrefab;
//            SecondsRemaining = secondsRemainging;
//            Name = markerPrefab.name;
//        }
//    }

//    private List<MotionMarker> _motionMarkersLocal;

//    private List<MotionMarker> _motionMarkersGameFrame;

//    private void Awake()
//    {
//        // useGUILayout = false;
//        Instance = this;
//    }

//    public static void Configure(Transform localPlayerTransform, float intervalLocal, float intervalGameFrame, float keepAliveLocal, float keepAliveGameFrame)
//    {
//        if (IsInitialized)
//        {
//            Instance._playerLocalTransform = localPlayerTransform;
//            Instance._intervalLocal = intervalLocal;
//            Instance._intervalGameFrame = intervalGameFrame;
//            Instance._keepAliveLocal = keepAliveLocal;
//            Instance._keepAliveGameFrame = keepAliveGameFrame;
//        }
//    }

//    private void Start()
//    {
//        _motionMarkersLocal = new List<MotionMarker>();
//        _motionMarkersGameFrame = new List<MotionMarker>();
//    }

//    private void OnGUI()
//    {
//        GUI.BeginGroup(new Rect(10, 10, 200, 200), "Visual Motion Debugger: " + _recordTimeLength.ToString(), "window");
//        _recordActive = GUI.Toggle(new Rect(4, 34, 64, 24), _recordActive, "Record");
//        if (Event.current.type == EventType.keyDown && Event.current.keyCode == KeyCode.ScrollLock)
//        {
//            _recordActive = !_recordActive;

//            if (_recordActive)
//            {
//                _startRecordTime = Time.realtimeSinceStartup;
//            }
//            else
//            {
//                _recordTimeLength = _stopRecordTime - _startRecordTime;
//            }
//        }

//        _recordLocal = GUI.Toggle(new Rect(4, 58, 64, 24), _recordLocal, "Mark Local Player");
//        _recordGameFrame = GUI.Toggle(new Rect(4, 82, 64, 24), _recordGameFrame, "Mark Game Frames");

//        GUI.EndGroup();
//    }

//    private void Update()
//    {
//        if (_recordActive)
//        {
//            // Ensure there is some data to record
//            if (_playerLocalTransform == null)
//            {
//                _recordActive = false;
//            }
//            else
//            {
//                _stopRecordTime = Time.realtimeSinceStartup;
//                if (_recordLocal)
//                {
//                    if (_currentLocalTime >= _intervalLocal)
//                    {
//                        // Update the local player transform markers
//                        _currentLocalTime = 0.0f;
//                        UpdatePlayerMotionMarker(_motionMarkersLocal, Color.green, _keepAliveLocal, _intervalLocal, _markerPrefabLocal, _playerLocalTransform.position, _playerLocalTransform.rotation, _markerLocalParent);
//                    }
//                    else
//                    {
//                        _currentLocalTime += Time.deltaTime;
//                    }
//                }

//                if (_recordGameFrame)
//                {
//                    if (_currentGameFrameTime >= _intervalGameFrame)
//                    {
//                        // Update the local player transform markers
//                        _currentGameFrameTime = 0.0f;
//                        UpdatePlayerMotionMarker(_motionMarkersGameFrame, Color.yellow, _keepAliveGameFrame, _intervalGameFrame, _markerPrefabGameFrame, ShortVector3.OptimizedVector3(_playerLocalTransform.position), _playerLocalTransform.rotation, _markerGameFrameParent);
//                    }
//                    else
//                    {
//                        _currentGameFrameTime += Time.deltaTime;
//                    }
//                }
//            }
//        }
//    }

//    private void UpdatePlayerMotionMarker(List<MotionMarker> motionMarkerList, Color markerColor, float keepAlive, float interval, GameObject markerTemplatePrefab, Vector3 position, Quaternion rotation, Transform parent)
//    {
//        // Create a motion marker
//        GameObject tempMarker = GameObject.Instantiate(markerTemplatePrefab, position, Quaternion.identity) as GameObject;
//        tempMarker.name = markerTemplatePrefab.name + "_" + Time.frameCount.ToString();
//        tempMarker.transform.parent = parent;
//        motionMarkerList.Add(new MotionMarker(Time.realtimeSinceStartup, tempMarker, keepAlive));

//        // Remove any old motion markers
//        for (int i = 0; i < motionMarkerList.Count; i++)
//        {
//            if (motionMarkerList[i].SecondsRemaining < 0)
//            {
//                GameObject.Destroy(motionMarkerList[i].MarkerPrefab);
//                motionMarkerList.RemoveAt(i);
//            }
//        }

//        // Update the time for each motion marker
//        for (int i = 0; i < motionMarkerList.Count; i++)
//        {
//            motionMarkerList[i].SecondsRemaining -= interval;
//            //motionMarkerList[i].MarkerPrefab.renderer.material.color = new Color(markerColor.r * motionMarkerList[i].SecondsRemaining, markerColor.g * motionMarkerList[i].SecondsRemaining, markerColor.b * motionMarkerList[i].SecondsRemaining);
//        }
//    }
//}
