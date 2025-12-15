using UnityEngine;
using System.Collections;

public class GuiText3D : MonoBehaviour
{
    public Font mFont;
    public System.String mText;
    public Camera mCamera;
    public Transform mTarget;

    public float mMaxDistance = 20;
    public float mLifeTime = 5;
    public Color mColor = Color.black;
    public bool mFadeOut = true;
    public Vector3 mFadeDirection = Vector2.up;

    private TextMesh _guiText;
    private Transform _transform;
    private Material _material;
    private Vector3 _viewportPosition;


    void Awake()
    {
        _transform = transform;
    }

    void Start()
    {
        _guiText = gameObject.AddComponent<TextMesh>();
        _guiText.anchor = TextAnchor.MiddleCenter;

        if (mCamera == null || mTarget == null || mFont == null)
        {
            Destroy(gameObject);
            return;
        }

        _guiText.font = mFont;
        _guiText.text = mText;

        if (mFont && mFont.material)
        {
            _guiText.GetComponent<Renderer>().material = mFont.material;
            _material = _guiText.GetComponent<Renderer>().material;
        }

        //StartCoroutine(startShowGuiText(mLifeTime));

        startColor = mColor;
        finalColor = mColor;
        if (mFadeOut) finalColor.a = 0;
    }
    float time = 0;
    Vector3 fadeDir = Vector3.zero;
    Color startColor, finalColor;

    void LateUpdate()
    {
        if (mCamera != null && mTarget != null && (mLifeTime < 0 || mLifeTime > time))
        {
            time += Time.deltaTime;
            //always keep the gui position in the viewport
            _viewportPosition = mCamera.WorldToViewportPoint(mTarget.localPosition);

            //lerp the color to transparent if fadeOut==true
            if (mFadeOut && mLifeTime > 0)
            {
                if (_material != null)
                    _material.color = Color.Lerp(startColor, finalColor, time / mLifeTime);
            }
            else
            {
                float dist = Mathf.Clamp01(_viewportPosition.z / mMaxDistance);
                if (_material != null)
                    _material.color = Color.Lerp(startColor, finalColor, dist);
            }

            fadeDir += Time.deltaTime * mFadeDirection;

            _transform.localPosition = _viewportPosition + fadeDir;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    IEnumerator startShowGuiText(float mLifeTime)
    {
        float time = 0;
        Vector3 fadeDir = Vector3.zero;

        Color startColor = mColor;
        Color finalColor = mColor;
        if (mFadeOut) finalColor.a = 0;

        while (mCamera != null && mTarget != null && (mLifeTime < 0 || mLifeTime > time))
        {
            time += Time.deltaTime;
            //always keep the gui position in the viewport
            _viewportPosition = mCamera.WorldToViewportPoint(mTarget.localPosition);

            //lerp the color to transparent if fadeOut==true
            if (mFadeOut && mLifeTime > 0)
            {
                _material.color = Color.Lerp(startColor, finalColor, time / mLifeTime);
            }
            else
            {
                float dist = Mathf.Clamp01(_viewportPosition.z / mMaxDistance);
                _material.color = Color.Lerp(startColor, finalColor, dist);
            }

            fadeDir += Time.deltaTime * mFadeDirection;

            _transform.localPosition = _viewportPosition + fadeDir;

            yield return new WaitForEndOfFrame();
        }

        Destroy(gameObject);
    }
}
