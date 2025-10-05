using UnityEngine;
using System.Collections;

public class TutorialAirlockFrontDoor : MonoBehaviour
{
    private bool _playerEntered;
    private TutorialWaypoint _waypoint;

    public bool PlayerEntered
    {
        get { return _playerEntered; }
    }

    public TutorialWaypoint Waypoint
    {
        get { return _waypoint; }
    }

    private void Awake()
    {
        _waypoint = GetComponent<TutorialWaypoint>();
    }

    private void OnTriggerEnter(Collider c)
    {
        if (_waypoint)
        {
            _waypoint.CanShow = false;
        }

        _playerEntered = true;
    }
}