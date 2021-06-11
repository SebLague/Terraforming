using UnityEngine;
using ComputeShaderUtility;

public class CloudManager : MonoBehaviour
{
	const string headerDecoration = " --- ";
	public enum Resolution
	{
		Full = 1,
		Half = 2,
		Quater = 4,
		Eighth = 8
	};

	[Header(headerDecoration + "Main" + headerDecoration)]
	public bool effectEnabled = true;
	public Resolution resolution;
	public Shader cloudShader;
	public Shader compositeShader;
	public float innerShellRadius = 400;
	public float outerShellRadius = 500;
	public float animSpeed;
	public Vector3 cloudTestParams;

	[Header("March settings" + headerDecoration)]
	public float minMainStepSize = 0;
	public int numStepsMain = 5;
	public int numStepsLight = 8;
	public float rayOffsetStrength;
	public Texture2D blueNoise;

	[Header(headerDecoration + "Base Shape" + headerDecoration)]
	public float cloudScale = 1;
	public float densityMultiplier = 1;
	public float densityOffset;
	public Vector3 shapeOffset;
	public Vector2 heightOffset;
	public Vector4 shapeNoiseWeights;

	[Header(headerDecoration + "Detail" + headerDecoration)]
	public float detailNoiseScale = 10;
	public float detailNoiseWeight = .1f;
	public Vector3 detailNoiseWeights;
	public Vector3 detailOffset;


	[Header(headerDecoration + "Lighting" + headerDecoration)]
	public float lightAbsorptionThroughCloud = 1;
	public float lightAbsorptionTowardSun = 1;
	[Range(0, 1)]
	public float darknessThreshold = .2f;
	[Range(0, 1)]
	public float forwardScattering = .83f;
	[Range(0, 1)]
	public float backScattering = .3f;
	[Range(0, 1)]
	public float baseBrightness = .8f;
	[Range(0, 1)]
	public float phaseFactor = .15f;

	[Header(headerDecoration + "Composite" + headerDecoration)]
	[Range(0, 8)]
	public int cloudBlurSize = 8;
	public float cloudBlurStrength = 5;

	Material cloudMaterial;
	Material compositeMaterial;
	NoiseGenerator noiseGenerator;

	RenderTexture cloudRender;
	GaussianBlur blur;

	void Init(RenderTexture src)
	{
		if (blur == null)
		{
			blur = new GaussianBlur();
		}

		EffectManager.CreateMaterial(ref cloudMaterial, cloudShader);
		EffectManager.CreateMaterial(ref compositeMaterial, compositeShader);

		int cloudRenderDiv = (int)resolution;
		int cloudRenderWidth = src.width / cloudRenderDiv;
		int cloudRenderHeight = src.height / cloudRenderDiv;
		ComputeHelper.CreateRenderTexture(ref cloudRender, cloudRenderWidth, cloudRenderHeight, src.filterMode, src.graphicsFormat, "Cloud Render");
		cloudRender.wrapMode = TextureWrapMode.Clamp;
		if (noiseGenerator == null)
		{
			noiseGenerator = FindObjectOfType<NoiseGenerator>();
		}
	}

	public void Render(RenderTexture src, RenderTexture dest)
	{
		if (!effectEnabled)
		{
			Graphics.Blit(src, dest);
			return;
		}

		Init(src);
		numStepsLight = Mathf.Max(1, numStepsLight);

		// Noise

		noiseGenerator.UpdateNoise();

		cloudMaterial.SetTexture("NoiseTex", noiseGenerator.shapeTexture);
		cloudMaterial.SetTexture("DetailNoiseTex", noiseGenerator.detailTexture);
		cloudMaterial.SetTexture("BlueNoise", blueNoise);

		cloudMaterial.SetFloat("scale", cloudScale);
		cloudMaterial.SetFloat("densityMultiplier", densityMultiplier);
		cloudMaterial.SetFloat("densityOffset", densityOffset);
		cloudMaterial.SetFloat("lightAbsorptionThroughCloud", lightAbsorptionThroughCloud);
		cloudMaterial.SetFloat("lightAbsorptionTowardSun", lightAbsorptionTowardSun);
		cloudMaterial.SetFloat("darknessThreshold", darknessThreshold);
		cloudMaterial.SetVector("params", cloudTestParams);
		cloudMaterial.SetFloat("rayOffsetStrength", rayOffsetStrength);

		cloudMaterial.SetFloat("detailNoiseScale", detailNoiseScale);
		cloudMaterial.SetFloat("detailNoiseWeight", detailNoiseWeight);
		cloudMaterial.SetVector("shapeOffset", shapeOffset);
		cloudMaterial.SetVector("detailOffset", detailOffset);
		cloudMaterial.SetVector("detailWeights", detailNoiseWeights);
		cloudMaterial.SetVector("shapeNoiseWeights", shapeNoiseWeights);
		cloudMaterial.SetVector("phaseParams", new Vector4(forwardScattering, backScattering, baseBrightness, phaseFactor));

		cloudMaterial.SetFloat("innerShellRadius", innerShellRadius);
		cloudMaterial.SetFloat("outerShellRadius", outerShellRadius);
		cloudMaterial.SetFloat("animSpeed", animSpeed);

		cloudMaterial.SetFloat("minMainStepSize", minMainStepSize);
		cloudMaterial.SetInt("numStepsLight", numStepsLight);
		cloudMaterial.SetInt("numStepsMain", numStepsMain);
		cloudMaterial.SetVector("dirToSun", EffectManager.DirToSun);

		// Bit does the following:
		// - sets _MainTex property on material to the source texture
		// - sets the render target to the destination texture
		// - draws a full-screen quad
		// This copies the src texture to the dest texture, with whatever modifications the shader makes
		Graphics.Blit(src, cloudRender, cloudMaterial);

		blur.Blur(cloudRender, cloudBlurSize, cloudBlurStrength);

		compositeMaterial.SetTexture("_Background", src);
		Graphics.Blit(cloudRender, dest, compositeMaterial);
	}


	void OnDestroy()
	{
		blur.Release();
	}

}