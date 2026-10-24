using UnityEngine;

// Attach this to the main camera to enable the UberStrike "RTX-style" post effect.
// Runs via OnRenderImage, so it's picked up by the existing image-effect toggle
// sweep in RenderSettingsController.EnableImageEffects when VideoPostProcessing
// flips. The shader is loaded from Resources so it survives the AssetBundle
// build path — no scene-serialized reference needed.
[RequireComponent(typeof(Camera))]
[ExecuteAlways]
[DisallowMultipleComponent]
public class PostProcessingRTX : MonoBehaviour
{
    private const string ShaderResourcePath = "Shaders/PostProcessingRTX";
    private const string ShaderName = "UberStrike/PostProcessingRTX";

    private Material _material;
    private bool _shaderUnavailable;

    // 0..1 scales every boost in the shader. Mapped from the Video slider (0..100).
    private float _strength = 0.6f;
    public float Strength
    {
        get { return _strength; }
        set
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(_strength, clamped)) return;
            _strength = clamped;
            ApplyStrength();
        }
    }

    private Material EnsureMaterial()
    {
        if (_material != null) return _material;
        if (_shaderUnavailable) return null;

        var shader = Resources.Load<Shader>(ShaderResourcePath);
        if (shader == null) shader = Shader.Find(ShaderName);
        if (shader == null || !shader.isSupported)
        {
            _shaderUnavailable = true;
            return null;
        }
        _material = new Material(shader);
        _material.hideFlags = HideFlags.HideAndDontSave;
        ApplyStrength();
        return _material;
    }

    private void ApplyStrength()
    {
        if (_material == null) return;
        // At strength=0 the shader passes through (every boost lerps from its
        // neutral value). At strength=1 the full effect kicks in — values match
        // the shader's Properties defaults.
        _material.SetFloat("_Saturation", Mathf.Lerp(1.0f, 1.25f, _strength));
        _material.SetFloat("_Contrast", Mathf.Lerp(1.0f, 1.12f, _strength));
        _material.SetFloat("_BloomThreshold", 0.65f);
        _material.SetFloat("_BloomIntensity", Mathf.Lerp(0f, 1.6f, _strength));
        _material.SetFloat("_Vignette", Mathf.Lerp(0f, 0.35f, _strength));
        _material.SetFloat("_Warmth", Mathf.Lerp(0f, 0.04f, _strength));
    }

    private void OnDisable()
    {
        if (_material != null)
        {
            DestroyImmediate(_material);
            _material = null;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        var mat = EnsureMaterial();
        if (mat == null)
        {
            Graphics.Blit(source, destination);
            return;
        }
        Graphics.Blit(source, destination, mat);
    }
}
