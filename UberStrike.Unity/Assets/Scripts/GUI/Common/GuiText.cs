using UnityEngine;
using System.Collections;

public class GuiText : MonoBehaviour
{
    [SerializeField]
    private Font _font;
    [SerializeField]
    private string _text;
    [SerializeField]
    private Color _color;
    [SerializeField]
    private Vector3 _offset;
    [SerializeField]
    private Transform _target;
    [SerializeField]
    private bool _hasTimeLimit = false;
    [SerializeField]
    private float _distanceCap = -1;

    private TextMesh _guiText;
    private Transform _transform;
    private Material _material;

    private float _visibleTime = 0;
    private bool _isVisible = true;

    void Awake()
    {
        _transform = transform;
    }

    void Start()
    {
        _guiText = gameObject.AddComponent<TextMesh>();
        _guiText.anchor = TextAnchor.MiddleCenter;
        _guiText.font = _font;
        _guiText.text = _text;
        if (_font && _font.material)
        {
            _guiText.GetComponent<Renderer>().material = _font.material;
            _material = _guiText.GetComponent<Renderer>().material;
        }
    }

    void LateUpdate()
    {
        if (Camera.main != null && _isVisible)
        {
            Vector3 viewportPosition = Camera.main.WorldToViewportPoint(_target.localPosition + _offset);
            _transform.position = viewportPosition;

            if (_hasTimeLimit)
            {
                _visibleTime -= Time.deltaTime;

                if (_visibleTime > 0)
                {
                    _color.a = _visibleTime;
                    _material.color = _color;
                }
                else
                {
                    _guiText.GetComponent<Renderer>().enabled = false;
                }
            }
            else
            {
                if (_distanceCap > 0)
                {
                    float dist = 1 - Mathf.Clamp01(viewportPosition.z / _distanceCap);
                    _color.a = dist;
                }
                _material.color = _color;
            }
        }
    }


    public void ShowText(int seconds)
    {
        _visibleTime = seconds;
    }

    public void ShowText()
    {
        ShowText(5);
    }

    public bool IsTextVisible
    {
        get { return _isVisible; }
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                _guiText.GetComponent<Renderer>().enabled = value;
            }
        }
    }
}
