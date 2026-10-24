using Cmune.Util;
using UnityEngine;

public class HeatWave : MonoBehaviour
{
    [SerializeField]
    private float _startSize = 0;
    [SerializeField]
    private float _maxSize = 0.05f;
    [SerializeField]
    private float _duration = 0.25f;
    [SerializeField]
    private float _distortion = 64;

    private Transform _transform;
    private Renderer _renderer;
    private float _elapsedTime;
    private float _normalizedTime;
    private float _s;

    private void Awake()
    {
        _transform = transform;
        _renderer = GetComponent<Renderer>();

        CmuneDebug.Assert(_renderer, "No Renderer attached to HeatWave script on GameObject " + gameObject.name);

        // Replace Y-up Plane mesh with Z-facing quad.
        // Original code uses LookRotation (Z toward camera) which requires Z-facing geometry.
        // Migration replaced the original mesh with Unity's default Y-up Plane, breaking orientation.
        // Quad is 10x10 units (same scale as default Plane) so existing _maxSize values work.
        var mf = GetComponent<MeshFilter>();
        if (mf != null)
        {
            Mesh quad = new Mesh();
            quad.name = "HeatWaveQuad";
            quad.vertices = new Vector3[] {
                new Vector3(-5f, -5f, 0f), new Vector3(5f, -5f, 0f),
                new Vector3(-5f, 5f, 0f),  new Vector3(5f, 5f, 0f)
            };
            quad.triangles = new int[] { 0, 1, 2, 1, 3, 2 };
            quad.normals = new Vector3[] {
                Vector3.forward, Vector3.forward,
                Vector3.forward, Vector3.forward
            };
            quad.uv = new Vector2[] {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 1f), new Vector2(1f, 1f)
            };
            mf.mesh = quad;
        }
    }

    /// <summary>
    /// Override prefab defaults for explosion-scale distortion.
    /// Called by ExplosionManager after instantiation.
    /// </summary>
    public void InitExplosion(float maxSize, float duration, float distortion)
    {
        _startSize = 0.1f;
        _maxSize = maxSize;
        _duration = duration;
        _distortion = distortion;
    }

    private Camera GetCamera()
    {
        if (LevelCamera.Exists && LevelCamera.Instance.MainCamera != null)
            return LevelCamera.Instance.MainCamera;
        return Camera.main;
    }

    private void Update()
    {
        if (_transform && _renderer)
        {
            Camera cam = GetCamera();
            if (cam == null) return;

            _elapsedTime = _elapsedTime + Time.deltaTime;
            _normalizedTime = _elapsedTime / _duration;

            //thought about this, and really, the wave would move linearly, fading in amplitude.
            _s = Mathf.Lerp(_startSize, _maxSize, _normalizedTime);

            _renderer.material.SetFloat("_BumpAmt", ((1 - _normalizedTime) * _distortion));

            _transform.localScale = new Vector3(_s, _s, _s);

            // Z-facing quad — LookRotation points Z at camera, mesh faces camera
            _transform.rotation = Quaternion.LookRotation(cam.transform.position - _transform.position);

            if (_elapsedTime > _duration)
            {
                GameObject.Destroy(gameObject);
            }
        }
    }
}