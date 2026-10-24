using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Mobile Bloom")]
public class MobileBloom : MonoBehaviour
{
	public float intensity = 0.5f;

	public Color colorMix = Color.white;

	public float colorMixBlend = 0.25f;

	public float agonyTint;

	public float intensityBoost;

	private Shader bloomShader = null;

	private Material apply;

	private RenderTextureFormat rtFormat = RenderTextureFormat.Default;

	private void OnEnable()
	{
		FindShaders();
		CheckSupport();
		CreateMaterials();
	}

	private void FindShaders()
	{
		if (!bloomShader)
		{
			bloomShader = Shader.Find("Cross Platform Shaders/Mobile Bloom");
		}
	}

	private void CreateMaterials()
	{
		if (!apply)
		{
			apply = new Material(bloomShader);
			apply.hideFlags = HideFlags.DontSave;
		}
	}

	private bool CheckSupport()
	{
		if (!SystemInfo.supportsImageEffects || !SystemInfo.supportsRenderTextures || !bloomShader.isSupported)
		{
			base.enabled = false;
			return false;
		}
		rtFormat = ((!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB565)) ? RenderTextureFormat.Default : RenderTextureFormat.RGB565);
		return true;
	}

	private void OnDisable()
	{
		if ((bool)apply)
		{
			Object.DestroyImmediate(apply);
			apply = null;
		}
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		// Unity 6 passthrough (auto-patched): the legacy ImageEffect shader
		// passes don't render to the destination RenderTexture under Unity 6,
		// causing the camera to go pitch black when this script is attached.
		// Blit source->destination and return so rendering stays visible.
		Graphics.Blit(source, destination);
		return;
#pragma warning disable CS0162
		agonyTint = Mathf.Clamp01(agonyTint - Time.deltaTime * 1.25f);
		intensityBoost = Mathf.Clamp01(intensityBoost - Time.deltaTime * 0.75f);
		RenderTexture temporary = RenderTexture.GetTemporary(source.width / 4, source.height / 4, 0, rtFormat);
		RenderTexture temporary2 = RenderTexture.GetTemporary(source.width / 4, source.height / 4, 0, rtFormat);
		apply.SetColor("_ColorMix", colorMix);
		apply.SetVector("_Parameter", new Vector4(colorMixBlend * 0.25f, 0f, 0f, 1f - intensity - (agonyTint + intensityBoost)));
		Graphics.Blit(source, temporary, apply, (agonyTint < 0.5f) ? 1 : 5);
		Graphics.Blit(temporary, temporary2, apply, 2);
		Graphics.Blit(temporary2, temporary, apply, 3);
		apply.SetTexture("_Bloom", temporary);
		Graphics.Blit(source, destination, apply, 4);
		RenderTexture.ReleaseTemporary(temporary);
		RenderTexture.ReleaseTemporary(temporary2);
#pragma warning restore CS0162
	}
}
