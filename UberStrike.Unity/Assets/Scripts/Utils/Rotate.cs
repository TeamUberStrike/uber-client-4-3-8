using UnityEngine;

public class Rotate : MonoBehaviour
{
    Transform _t;

	// Use this for initialization
	void Start () {
        _t = transform;
	}
	
	// Update is called once per frame
	void Update () {
        _t.Rotate(Vector3.up, Time.deltaTime * 2, Space.Self);
	}

    void OnDrawGizmos()
    {
        if (!_t) _t = transform;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(_t.position, _t.forward);
    }
}
