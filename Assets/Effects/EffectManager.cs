using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{

	public bool atmosphereEnabled = true;
	public bool cloudsEnabled = true;
	public bool underwaterEnabled = true;
	public bool antiAliasingEnabled = true;
	public AtmosphereSettings atmosphereSettings;
	Water waterSettings;

	public Shader depthShader;
	public Shader underwaterShader;
	public Shader hudShader;

	public Camera waterDepthCam;

	RenderTexture waterDepthTex;
	Material atmosphereMat;
	Material underwaterMat;
	Material hudMat;

	RenderTexture blurredTexture;
	ComputeShaderUtility.GaussianBlur gaussianBlur;
	CloudManager cloudManager;
	FXAAEffect fxaa;
	static Transform sunTransform;

	void Awake()
	{
		sunTransform = GameObject.FindWithTag("Sun").transform;
		Camera.main.depthTextureMode = DepthTextureMode.Depth;

		atmosphereSettings.FlagForUpdate();
		waterDepthCam.depthTextureMode = DepthTextureMode.Depth;

		gaussianBlur = new ComputeShaderUtility.GaussianBlur();

	}

	void RenderAtmosphere(RenderTexture source, RenderTexture target)
	{
		if (atmosphereEnabled)
		{
			waterDepthCam.RenderWithShader(depthShader, "");
			atmosphereSettings.SetProperties(atmosphereMat);
			Graphics.Blit(source, target, atmosphereMat);
		}
		else
		{
			Graphics.Blit(source, target);
		}
	}

	void RenderClouds(RenderTexture source, RenderTexture target)
	{
		if (cloudsEnabled)
		{
			cloudManager.Render(source, target);
		}
		else
		{
			Graphics.Blit(source, target);
		}
	}

	void RenderUnderwater(RenderTexture source, RenderTexture target)
	{
		if (underwaterEnabled)
		{
			ComputeHelper.CreateRenderTexture(ref blurredTexture, source);
			gaussianBlur.Blur(source, blurredTexture, waterSettings.blurSize, waterSettings.blurStrength);

			underwaterMat.SetTexture("_BlurredTexture", blurredTexture);
			Graphics.Blit(source, target, underwaterMat);
		}
		else
		{
			Graphics.Blit(source, target);
		}
	}


	public void HandleEffects(RenderTexture source, RenderTexture target)
	{
		Init();

		// -------- Atmosphere --------
		RenderTexture atmosphereComposite = RenderTexture.GetTemporary(source.descriptor);
		RenderAtmosphere(source, atmosphereComposite);

		// -------- Clouds ---------
		RenderTexture cloudComposite = RenderTexture.GetTemporary(source.descriptor);
		RenderClouds(atmosphereComposite, cloudComposite);

		// -------- Underwater --------
		RenderTexture underwaterComposite = RenderTexture.GetTemporary(source.descriptor);
		RenderUnderwater(cloudComposite, underwaterComposite);

		// -------- Anti-Aliasing --------
		RenderTexture antiAliasedResult = RenderTexture.GetTemporary(source.descriptor);
		if (antiAliasingEnabled)
		{
			fxaa.Render(underwaterComposite, antiAliasedResult);
		}
		else
		{
			Graphics.Blit(underwaterComposite, antiAliasedResult);
		}

		// -------- HUD --------
		Graphics.Blit(antiAliasedResult, target, hudMat);

		// -------- Release --------
		RenderTexture.ReleaseTemporary(atmosphereComposite);
		RenderTexture.ReleaseTemporary(cloudComposite);
		RenderTexture.ReleaseTemporary(underwaterComposite);
		RenderTexture.ReleaseTemporary(antiAliasedResult);

	}

	void Init()
	{
		CreateMaterial(ref atmosphereMat, atmosphereSettings.shader);
		CreateMaterial(ref underwaterMat, underwaterShader);
		CreateMaterial(ref hudMat, hudShader);

		if (waterDepthTex == null || waterDepthTex.width != Screen.width || waterDepthTex.height != Screen.height)
		{
			if (waterDepthTex)
			{
				waterDepthTex.Release();
			}
			waterDepthTex = new RenderTexture(Screen.width, Screen.height, 32, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat);
			waterDepthTex.Create();
			waterDepthCam.targetTexture = waterDepthTex;
		}

		if (waterSettings == null)
		{
			waterSettings = FindObjectOfType<Water>();
		}

		waterSettings?.SetUnderwaterProperties(underwaterMat);

		if (cloudManager == null)
		{
			cloudManager = FindObjectOfType<CloudManager>();
		}

		if (fxaa == null)
		{
			fxaa = FindObjectOfType<FXAAEffect>();
		}
	}


	public static void CreateMaterial(ref Material material, Shader shader)
	{
		if (material == null || material.shader != shader)
		{
			material = new Material(shader);
		}
	}

	void OnDestroy()
	{
		gaussianBlur.Release();
		ComputeHelper.Release(waterDepthTex, blurredTexture);
	}

	public static Vector3 DirToSun {
		get {
			return -sunTransform.forward;
		}
	}
}
