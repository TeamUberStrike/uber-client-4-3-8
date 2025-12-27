using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[AddComponentMenu("Image Effects/Silhouette Outlined")]
public class SilhouetteOutlined : ImageEffectBase
{
    protected Material GaussianBlurMaterial
    {
        get
        {
            if (_gaussianBlurMaterial == null)
            {
                _gaussianBlurMaterial = new Material(gaussianBlurShader);
                _gaussianBlurMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            return _gaussianBlurMaterial;
        }
    }

    protected GameObject ShaderCamera
    {
        get
        {
            if (!_shaderCamera)
            {
                _shaderCamera = new GameObject("ShaderCamera", typeof(Camera));
                _shaderCamera.GetComponent<Camera>().enabled = false;
                _shaderCamera.hideFlags = HideFlags.HideAndDontSave;
            }
            return _shaderCamera;
        }
    }

    new void OnDisable()
    {
        base.OnDisable();
        DestroyImmediate(_shaderCamera);
        if (_glowTexture != null)
        {
            RenderTexture.ReleaseTemporary(_glowTexture);
            _glowTexture = null;
        }
    }

    private void OnPreRender()
    {
        if (!enabled || !gameObject.active)
        {
            return;
        }

        CleanRenderTextures();

        Camera cam = ShaderCamera.GetComponent<Camera>();
        cam.CopyFrom(GetComponent<Camera>());
        cam.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        _maskTexture = RenderTexture.GetTemporary((int)GetComponent<Camera>().pixelWidth, (int)GetComponent<Camera>().pixelHeight, 16);
        cam.targetTexture = _maskTexture;
        cam.RenderWithShader(objectMaskShader, "Outline");

        UpdateGlowTextureSize(GetComponent<Camera>().pixelWidth, GetComponent<Camera>().pixelHeight);
        _glowTexture = RenderTexture.GetTemporary(_glowTexWidth, _glowTexHeight, 16);
        cam.targetTexture = _glowTexture;
        cam.RenderWithShader(generateGlowTextureShader, "Outline");
    }

    private void UpdateGlowTextureSize(float cameraWidth, float cameraHeight)
    {
        _glowTexWidth = (int)cameraWidth;
        _glowTexHeight = (int)cameraHeight;
        float ratio = cameraWidth / cameraHeight;
        if (cameraWidth > 256 && cameraWidth < 512)
        {
            _glowTexWidth = 256;
            _glowTexHeight = (int)(_glowTexWidth / ratio);
        }
        if (cameraWidth > 512)
        {
            _glowTexWidth = 512;
            _glowTexHeight = (int)(_glowTexWidth / ratio);
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        GaussianBlur(source, destination);
        //NoBlur(source, destination);
        CleanRenderTextures();
    }

    private void GaussianBlur(RenderTexture source, RenderTexture dest)
    {
        RenderTexture buffer = RenderTexture.GetTemporary(source.width, source.height);
        GaussianBlurMaterial.SetFloat("_TexWidth", _glowTexWidth);
        GaussianBlurMaterial.SetFloat("_TexHeight", _glowTexHeight);
        Graphics.Blit(_glowTexture, buffer, GaussianBlurMaterial, 0);
        Graphics.Blit(buffer, _glowTexture, GaussianBlurMaterial, 1);
        RenderTexture.ReleaseTemporary(buffer);

        material.SetTexture("_GlowTex", _glowTexture);
        material.SetTexture("_MaskTex", _maskTexture);
        material.SetFloat("_IsUseGlobalColor", isUseGlobalColor ? 1.0f : 0.0f);
        material.SetColor("_GlobalOutlineColor", globalOutlineColor);
        Graphics.Blit(source, dest, material);
    }

    private void NoBlur(RenderTexture source, RenderTexture dest)
    {
        material.SetTexture("_GlowTex", _glowTexture);
        material.SetTexture("_MaskTex", _maskTexture);
        material.SetFloat("_IsUseGlobalColor", isUseGlobalColor ? 1.0f : 0.0f);
        material.SetColor("_GlobalOutlineColor", globalOutlineColor);
        Graphics.Blit(source, dest, material);
    }

    private void CleanRenderTextures()
    {
        if (_glowTexture != null)
        {
            RenderTexture.ReleaseTemporary(_glowTexture);
            _glowTexture = null;
        }

        if (_maskTexture != null)
        {
            RenderTexture.ReleaseTemporary(_maskTexture);
            _maskTexture = null;
        }
    }

    [SerializeField]
    private Shader generateGlowTextureShader;
    [SerializeField]
    private Shader gaussianBlurShader;
    [SerializeField]
    private Shader objectMaskShader;
    [SerializeField]
    private Color globalOutlineColor;
    [SerializeField]
    private bool isUseGlobalColor;

    private Material _gaussianBlurMaterial;
    private RenderTexture _glowTexture;
    private RenderTexture _maskTexture;
    private GameObject _shaderCamera;

    private int _glowTexWidth;
    private int _glowTexHeight;
}
