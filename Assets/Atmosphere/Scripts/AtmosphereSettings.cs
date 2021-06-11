using UnityEngine;
using static UnityEngine.Mathf;

[CreateAssetMenu(menuName = "Atmosphere")]
public class AtmosphereSettings : ScriptableObject
{

	public Shader shader;
	public Shader atmosphereShader;
	public ComputeShader opticalDepthCompute;
	public int textureSize = 256;

	public int inScatteringPoints = 10;
	public int opticalDepthPoints = 10;
	public float densityFalloff = 0.25f;

	public Vector3 wavelengths = new Vector3(700, 530, 460);

	public Vector4 testParams = new Vector4(7, 1.26f, 0.1f, 3);
	public float scatteringStrength = 20;
	public float intensity = 1;



	public float planetRadius;
	public float atmosphereRadius;

	[Header("Dither")]
	public float ditherStrength = 0.8f;
	public float ditherScale = 4;
	public Texture2D blueNoise;

	[Header("Mixing")]
	public float originalColourStrength = 180;
	public float overlayStrength = 0.5f;

	//public float atmosphereScale = 0.5f;
	//public float bodyRadius = 100;

	RenderTexture opticalDepthTexture;
	bool settingsUpToDate;

	public void FlagForUpdate()
	{
		settingsUpToDate = false;
	}


	public void SetProperties(Material material)
	{
		if (!settingsUpToDate || Application.isEditor)
		{
			if (!settingsUpToDate)
			{
				PrecomputeOutScattering();
			}
			//float atmosphereRadius = (1 + atmosphereScale) * bodyRadius;

			material.SetVector("params", testParams);
			material.SetInt("numInScatteringPoints", inScatteringPoints);
			material.SetInt("numOpticalDepthPoints", opticalDepthPoints);
			material.SetFloat("atmosphereRadius", atmosphereRadius);
			material.SetFloat("planetRadius", planetRadius);
			material.SetFloat("densityFalloff", densityFalloff);
			material.SetFloat("originalColourStrength", originalColourStrength);
			material.SetFloat("overlayStrength", overlayStrength);

			material.SetVector("dirToSun", EffectManager.DirToSun);
			material.SetVector("planetCentre", Vector3.zero);
			material.SetFloat("oceanRadius", 0);

			// Strength of (rayleigh) scattering is inversely proportional to wavelength^4
			float scatterX = Pow(400 / wavelengths.x, 4);
			float scatterY = Pow(400 / wavelengths.y, 4);
			float scatterZ = Pow(400 / wavelengths.z, 4);
			material.SetVector("scatteringCoefficients", new Vector3(scatterX, scatterY, scatterZ) * scatteringStrength);
			material.SetFloat("intensity", intensity);
			material.SetFloat("ditherStrength", ditherStrength);
			material.SetFloat("ditherScale", ditherScale);
			material.SetTexture("_BlueNoise", blueNoise);


			material.SetTexture("_BakedOpticalDepth", opticalDepthTexture);

			settingsUpToDate = true;
		}
	}

	void PrecomputeOutScattering()
	{
		if (!settingsUpToDate || opticalDepthTexture == null || !opticalDepthTexture.IsCreated())
		{
			ComputeHelper.CreateRenderTexture(ref opticalDepthTexture, textureSize, textureSize, FilterMode.Bilinear, ComputeHelper.defaultGraphicsFormat);
			opticalDepthCompute.SetTexture(0, "Result", opticalDepthTexture);
			opticalDepthCompute.SetInt("textureSize", textureSize);
			opticalDepthCompute.SetInt("numOutScatteringSteps", opticalDepthPoints);
			opticalDepthCompute.SetFloat("atmosphereRadius", atmosphereRadius / planetRadius);
			opticalDepthCompute.SetFloat("densityFalloff", densityFalloff);
			opticalDepthCompute.SetVector("params", testParams);
			ComputeHelper.Dispatch(opticalDepthCompute, textureSize, textureSize);
		}

	}

	void OnValidate()
	{
		FlagForUpdate();
	}
}