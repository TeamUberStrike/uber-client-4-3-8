using UnityEngine;

public class MuzzleHeatWave : BaseWeaponEffect
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

        Debug.Log("[MuzzleHeatWave] Awake() go=" + gameObject.name +
            " renderer=" + (_renderer != null) +
            " parent=" + (transform.parent != null ? transform.parent.name : "null") +
            " layer=" + gameObject.layer);

        if (_renderer == null)
            Debug.LogWarning("[MuzzleHeatWave] No Renderer on " + gameObject.name);

        // Original prefab values from 3.5.5.
        _maxSize = 0.25f;
        _duration = 0.25f;

        // Replace Y-up Plane mesh with Z-facing quad.
        // Migration replaced the original mesh with Unity's default Y-up Plane.
        // LookRotation (Z toward camera) requires Z-facing geometry — a Y-up Plane
        // would appear edge-on to the camera. The quad is 10x10 units (same as Plane)
        // so existing _maxSize scale values work identically.
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

    private void Start()
    {
        // DO NOT change layer. This object must stay on layer 19 (Weapons) where
        // WeaponSlot.SetLayerRecursively places it. The weapon camera renders it:
        // - GrabPass captures the scene (already rendered by main camera in framebuffer)
        // - HeatDistort_3.0 shader bends the grabbed scene texture
        // - Distortion is visible on top of/around the weapon barrel
        // Moving to layer 0 (main camera) was WRONG — main camera renders distortion
        // BEFORE weapon camera draws, then weapon camera paints over it, hiding the effect.
        if (_renderer != null)
        {
            _renderer.enabled = false;
        }
        this.enabled = false;
    }

    private void OnEnable()
    {
        // Match 4.7 behavior: hide effects when component is re-enabled
        // (e.g., weapon swap re-activates the GO)
        Hide();
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

            _s = Mathf.Lerp(_startSize, _maxSize, _normalizedTime);

            _renderer.material.SetFloat("_BumpAmt", ((1 - _normalizedTime) * _distortion));

            _transform.localScale = new Vector3(_s, _s, _s);

            // Z-facing quad — LookRotation points Z at camera, mesh faces camera
            _transform.rotation = Quaternion.LookRotation(cam.transform.position - _transform.position);

            if (_elapsedTime > _duration)
            {
                _transform.localScale = new Vector3(0, 0, 0);
                _renderer.enabled = false;
                this.enabled = false;
            }
        }
    }

    public override void OnShoot()
    {
        Debug.Log("[MuzzleHeatWave] OnShoot() go=" + gameObject.name +
            " renderer=" + (_renderer != null) +
            " layer=" + gameObject.layer);

        // PS-based fallback: emit a small HeatWave particle on the main camera's
        // HeatWave PS (layer 12). This is guaranteed visible — the mesh-based approach
        // on the weapon camera (layer 19) fails because GrabPass doesn't work correctly
        // on secondary cameras in Unity 2022.
        // Circle around the muzzle flash: size=0.5 surrounds the flash, life=0.25 = brief.
        ParticleEffectController.ShowHeatwaveEffect(_transform.position, 0.5f, 0.25f);

        // Mesh-based approach (weapon camera) — kept for forward compatibility
        if (_renderer != null && SystemInfo.supportsImageEffects)
        {
            Camera cam = GetCamera();
            if (cam == null) return;

            _elapsedTime = 0f;
            _transform.rotation = Quaternion.LookRotation(cam.transform.position - _transform.position);

            _renderer.enabled = true;
            this.enabled = true;
        }
    }

    public override void OnPostShoot()
    {
    }

    public override void Hide()
    {
        if (!_renderer)
            _renderer = GetComponent<Renderer>();

        if (_renderer)
            _renderer.enabled = false;
    }
}
