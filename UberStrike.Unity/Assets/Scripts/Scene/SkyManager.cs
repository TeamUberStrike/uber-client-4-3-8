using UnityEngine;

public class SkyManager : MonoBehaviour
{
    [SerializeField]
    private float _dayNightCycle;

    [SerializeField]
    private float _sunsetOffset;

    [SerializeField]
    private float _sunsetVisibility;

    [SerializeField]
    private Color _daySkyColor;

    [SerializeField]
    private Color _horizonColor;

    [SerializeField]
    private Color _sunsetColor;

    private Vector2 _dayCloudMoveVector = new Vector2(0, 0);
    private Vector2 _dayCloudHorizonMoveVector = new Vector2(0, 0);
    private float _cloudXAxisRot = .005f;
    private float _cloudYAxisRot = .005f;
    private float _cloudXAxisRotIndex = .001f;
    private float _cloudYAxisRotIndex = .001f;
    private Material _skyMaterial;

    public float DayNightCycle
    {
        get { return _dayNightCycle; }
        set { _dayNightCycle = value; }
    }

    public float CloudXAxisRot
    {
        get { return _cloudXAxisRot; }
        set { _cloudXAxisRot = value; }
    }

    public float CloudYAxisRot
    {
        get { return _cloudYAxisRot; }
        set { _cloudYAxisRot = value; }
    }

    void OnEnable()
    {
        _skyMaterial = new Material(renderer.material);
    }

    void OnDisable()
    {
        renderer.material = _skyMaterial;
    }

    void Update()
    {
        _dayCloudMoveVector.x += Time.deltaTime * _cloudXAxisRot;
        _dayCloudHorizonMoveVector.y += Time.deltaTime * _cloudYAxisRot;

        if (_dayCloudMoveVector.x > 1)
        {
            _dayCloudMoveVector.x = 0;
            if (_cloudXAxisRot > .008f)
            {
                _cloudXAxisRotIndex = -.001f;
            }
            if (_cloudXAxisRot < .002f)
            {
                _cloudXAxisRotIndex = .001f;
            }
            _cloudXAxisRot += _cloudXAxisRotIndex;
        }

        if (_dayCloudHorizonMoveVector.y > 1)
        {
            _dayCloudHorizonMoveVector.y = 0;
            if (_cloudYAxisRot > .008f)
            {
                _cloudYAxisRotIndex = -.001f;
            }
            if (_cloudYAxisRot < .002f)
            {
                _cloudYAxisRotIndex = .001f;
            }
            _cloudYAxisRot += _cloudYAxisRotIndex;
        }

        renderer.material.SetTextureOffset("_DayCloudTex", _dayCloudMoveVector);
        renderer.material.SetTextureOffset("_NightCloudTex", _dayCloudHorizonMoveVector);

        _dayNightCycle = Mathf.Clamp01(_dayNightCycle);
        renderer.material.SetFloat("_DayNightCycle", Mathf.Clamp01(_dayNightCycle));

        _sunsetOffset = Mathf.Clamp01(_sunsetOffset);
        renderer.material.SetFloat("_SunsetOffset", Mathf.Clamp01(_sunsetOffset));

        _sunsetVisibility = Mathf.Clamp01(_sunsetVisibility);
        renderer.material.SetFloat("_SunsetVisibility", _sunsetVisibility);

        renderer.material.SetColor("_HorizonColor", _horizonColor);
        renderer.material.SetColor("_DaySkyColor", _daySkyColor);
        renderer.material.SetColor("_SunSetColor", _sunsetColor);
    }
}

