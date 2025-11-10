using UnityEngine;
using System.Collections;

public class LookToTarget : MonoBehaviour
{
    private Transform _follow;
 
    private Transform transformComponent;

    void Start()
    {
        transformComponent = transform;
    }

    public Transform follow
    {
        get { return _follow; }
        set
        {
            _follow = value;
            this.enabled = (_follow != null);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_follow != null)
        {
            transformComponent.position = Vector3.Lerp(transformComponent.position, _follow.position, Time.deltaTime);
            //transformComponent.rotation = Quaternion.Slerp(transformComponent.rotation, _follow.rotation, 0.8F * Time.deltaTime);
            transformComponent.rotation = Quaternion.Slerp(transformComponent.rotation, Quaternion.LookRotation(_follow.position - transformComponent.position), 0.8F * Time.deltaTime);
        }
        else
        {
            this.enabled = false;
        }
    }
}
