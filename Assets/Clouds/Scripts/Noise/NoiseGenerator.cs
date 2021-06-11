using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseGenerator : MonoBehaviour
{
	public const string detailNoiseName = "DetailNoise";
	public const string shapeNoiseName = "ShapeNoise";

	public enum CloudNoiseType { Shape, Detail }
	public enum TextureChannel { R, G, B }

	[Header("Editor Settings")]
	public CloudNoiseType activeTextureType;
	public TextureChannel activeChannel;

	public ComputeShader noiseCompute;

	[Header("Shape Settings")]
	[Min(1)] public int shapeResolution = 132;
	public WorleyNoiseSettings shapeR;
	public WorleyNoiseSettings shapeG;
	public WorleyNoiseSettings shapeB;

	[Header("Detail Settings")]
	[Min(1)] public int detailResolution = 32;
	public WorleyNoiseSettings detailR;
	public WorleyNoiseSettings detailG;
	public WorleyNoiseSettings detailB;

	[Header("Viewer Settings")]
	public bool viewerEnabled;
	public bool viewerGreyscale = true;
	public bool viewerShowAllChannels;
	[Range(0, 1)]
	public float viewerSliceDepth;
	[Range(1, 5)]
	public float viewerTileAmount = 1;
	[Range(0, 1)]
	public float viewerSize = 1;

	// Internal
	List<ComputeBuffer> buffersToRelease;
	bool settingsChangedSinceLastUpdate;

	[HideInInspector]
	public bool showSettingsEditor = true;

	public RenderTexture shapeTexture;
	public RenderTexture detailTexture;

	void Awake()
	{
		settingsChangedSinceLastUpdate = true;

	}

	public void UpdateNoise()
	{
		CreateTexture(ref shapeTexture, shapeResolution, shapeNoiseName);
		CreateTexture(ref detailTexture, detailResolution, detailNoiseName);

		if (settingsChangedSinceLastUpdate && noiseCompute)
		{
			settingsChangedSinceLastUpdate = false;

			UpdateSettings(shapeR, shapeTexture, new Vector3(1, 0, 0));
			UpdateSettings(shapeG, shapeTexture, new Vector3(0, 1, 0));
			UpdateSettings(shapeB, shapeTexture, new Vector3(0, 0, 1));

			UpdateSettings(detailR, detailTexture, new Vector3(1, 0, 0));
			UpdateSettings(detailG, detailTexture, new Vector3(0, 1, 0));
			UpdateSettings(detailB, detailTexture, new Vector3(0, 0, 1));

		}


	}

	void UpdateSettings(WorleyNoiseSettings settings, RenderTexture texture, Vector3 channelMask)
	{
		if (settings == null)
		{
			return;
		}

		buffersToRelease = new List<ComputeBuffer>();

		int texSize = texture.width;

		// Set values:
		noiseCompute.SetFloat("persistence", settings.persistence);
		noiseCompute.SetInt("resolution", texSize);
		noiseCompute.SetVector("channelMask", channelMask);

		// Set noise gen kernel data:
		noiseCompute.SetTexture(0, "Result", texture);
		var minMaxBuffer = CreateBuffer(new int[] { int.MaxValue, 0 }, sizeof(int), "minMax", 0);
		UpdateWorley(settings);
		//var noiseValuesBuffer = CreateBuffer (activeNoiseValues, sizeof (float) * 4, "values");

		// Dispatch noise gen kernel
		ComputeHelper.Dispatch(noiseCompute, texSize, texSize, texSize, 0);

		// Set normalization kernel data:
		noiseCompute.SetBuffer(1, "minMax", minMaxBuffer);
		noiseCompute.SetTexture(1, "Result", texture);
		// Dispatch normalization kernel
		ComputeHelper.Dispatch(noiseCompute, texSize, texSize, texSize, 1);

		// Release buffers
		foreach (var buffer in buffersToRelease)
		{
			buffer.Release();
		}
	}

	public WorleyNoiseSettings ActiveSettings
	{
		get
		{
			if (activeChannel == TextureChannel.R)
			{
				return (activeTextureType == CloudNoiseType.Shape) ? shapeR : detailR;
			}
			if (activeChannel == TextureChannel.G)
			{
				return (activeTextureType == CloudNoiseType.Shape) ? shapeG : detailG;
			}
			else
			{
				return (activeTextureType == CloudNoiseType.Shape) ? shapeB : detailB;
			}
		}
	}

	void UpdateWorley(WorleyNoiseSettings settings)
	{
		var prng = new System.Random(settings.seed);
		CreateWorleyPointsBuffer(prng, settings.numDivisionsA, "pointsA");
		CreateWorleyPointsBuffer(prng, settings.numDivisionsB, "pointsB");
		CreateWorleyPointsBuffer(prng, settings.numDivisionsC, "pointsC");

		noiseCompute.SetInt("numCellsA", settings.numDivisionsA);
		noiseCompute.SetInt("numCellsB", settings.numDivisionsB);
		noiseCompute.SetInt("numCellsC", settings.numDivisionsC);
		noiseCompute.SetBool("invertNoise", settings.invert);
		noiseCompute.SetInt("tile", settings.tile);
		noiseCompute.SetBool("enabled", settings.enabled);

	}

	void CreateWorleyPointsBuffer(System.Random prng, int numCellsPerAxis, string bufferName)
	{
		var points = new Vector3[numCellsPerAxis * numCellsPerAxis * numCellsPerAxis];
		float cellSize = 1f / numCellsPerAxis;

		for (int x = 0; x < numCellsPerAxis; x++)
		{
			for (int y = 0; y < numCellsPerAxis; y++)
			{
				for (int z = 0; z < numCellsPerAxis; z++)
				{
					float randomX = (float)prng.NextDouble();
					float randomY = (float)prng.NextDouble();
					float randomZ = (float)prng.NextDouble();
					Vector3 randomOffset = new Vector3(randomX, randomY, randomZ) * cellSize;
					Vector3 cellCorner = new Vector3(x, y, z) * cellSize;

					int index = x + numCellsPerAxis * (y + z * numCellsPerAxis);
					points[index] = cellCorner + randomOffset;
				}
			}
		}

		CreateBuffer(points, sizeof(float) * 3, bufferName);
	}

	// Create buffer with some data, and set in shader. Also add to list of buffers to be released
	ComputeBuffer CreateBuffer(System.Array data, int stride, string bufferName, int kernel = 0)
	{
		var buffer = new ComputeBuffer(data.Length, stride, ComputeBufferType.Structured);
		buffersToRelease.Add(buffer);
		buffer.SetData(data);
		noiseCompute.SetBuffer(kernel, bufferName, buffer);
		return buffer;
	}

	public Vector4 ChannelMask
	{
		get
		{
			Vector4 channelWeight = new Vector4(
				(activeChannel == NoiseGenerator.TextureChannel.R) ? 1 : 0,
				(activeChannel == NoiseGenerator.TextureChannel.G) ? 1 : 0,
				(activeChannel == NoiseGenerator.TextureChannel.B) ? 1 : 0,
				0
			);
			return channelWeight;
		}
	}

	void CreateTexture(ref RenderTexture texture, int resolution, string name)
	{
		var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm;
		if (texture == null || !texture.IsCreated() || texture.width != resolution || texture.height != resolution || texture.volumeDepth != resolution || texture.graphicsFormat != format)
		{

			if (texture != null)
			{
				texture.Release();
			}
			texture = new RenderTexture(resolution, resolution, 0);
			texture.graphicsFormat = format;
			texture.volumeDepth = resolution;
			texture.enableRandomWrite = true;
			texture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
			texture.name = name;

			texture.Create();
			//Load(name, texture);
		}
		texture.wrapMode = TextureWrapMode.Repeat;
		texture.filterMode = FilterMode.Bilinear;
	}

	public void ManualUpdate()
	{
		settingsChangedSinceLastUpdate = true;
		UpdateNoise();
	}

	public void ActiveNoiseSettingsChanged()
	{
		settingsChangedSinceLastUpdate = true;
	}

	public void DebugView(Material mat)
	{
		mat.SetTexture("NoiseTex", (activeTextureType == CloudNoiseType.Shape) ? shapeTexture : detailTexture);
		mat.SetFloat("debugNoiseSliceDepth", viewerSliceDepth);
		mat.SetFloat("debugTileAmount", viewerTileAmount);
		mat.SetFloat("viewerSize", viewerSize);
		mat.SetVector("debugChannelWeight", ChannelMask);
		mat.SetInt("debugGreyscale", (viewerGreyscale) ? 1 : 0);
		mat.SetInt("debugShowAllChannels", (viewerShowAllChannels) ? 1 : 0);
	}

}