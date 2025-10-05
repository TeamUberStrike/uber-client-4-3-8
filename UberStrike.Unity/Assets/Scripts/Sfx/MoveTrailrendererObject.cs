using UnityEngine;

/// <summary>
/// 
/// </summary>
public class MoveTrailrendererObject : MonoBehaviour
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="muzzlePosition"></param>
    /// <param name="distance"></param>
    public void MoveTrail(Vector3 destination, Vector3 muzzlePosition, float distance)
    {
        if (_lineRenderer != null)
        {
            _alpha = 1;
            _move = true;
            _lineRenderer.SetPosition(0, muzzlePosition);
            _lineRenderer.SetPosition(1, destination);
            _timeToArrive = Time.time + _duration;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private void Update()
    {
        if (_move)
        {
            _locationOnPath = 1 - (_timeToArrive - Time.time);
            _alpha = Mathf.Lerp(_alpha, 0, _locationOnPath);
            Color tmp = _lineRenderer.material.GetColor("_TintColor");
            tmp.a = _alpha;
            _lineRenderer.materials[0].SetColor("_TintColor", tmp);

            if (Time.time >= _timeToArrive)
            {
                _move = false;
                Destroy( gameObject );
            }
        }
    }

    #region Fields
    [SerializeField]
    private LineRenderer _lineRenderer;
    [SerializeField]
    private float _duration = 0.1f;

    private float _locationOnPath = 0;
    private bool _move = false;
    private float _timeToArrive = 1f;
    private float _alpha = 1f;
    #endregion
}