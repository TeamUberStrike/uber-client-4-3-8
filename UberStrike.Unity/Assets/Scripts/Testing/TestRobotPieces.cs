using UnityEngine;
using System.Collections;

public class TestRobotPieces : MonoBehaviour {

	// Use this for initialization
	void Start () 
    {
	
	}

    void OnGUI()
    {
        if (GUILayout.Button("explode"))
        {
            var pieces = GameObject.Instantiate(robotPieces, Vector3.zero, Quaternion.identity) as GameObject;
            if (pieces != null)
            {
                RobotPiecesLogic robotPiecesLogic = pieces.GetComponentInChildren<RobotPiecesLogic>();
                robotPiecesLogic.ExplodeRobot(pieces, 2);
            }
        }
    }
	
	void Update () {
	
	}

    public GameObject robotPieces;
}
