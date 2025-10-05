using UnityEngine;

public class SelfDestroy : MonoBehaviour
{
    [SerializeField]
    private float _destroyInSeconds = 1;

    private void Start()
    {
        GameObject.Destroy(gameObject, _destroyInSeconds);
    }
}
