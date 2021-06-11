using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Water : MonoBehaviour
{
	public float radius = 210;
	public int resolution = 10;

	public float edgeFade = 0.1f;

	public Color shallowCol;
	public Color deepCol;
	public float colDepthFactor;
	public float smoothness;

	public Texture2D waveNormalA;
	public Texture2D waveNormalB;
	public Texture2D foamNoise;

	public Vector3 testParams;
	public float alphaFresnelPow;

	[Header("Underwater")]
	[Range(0.1f, 1)]
	public float maxVisibility;
	public float underwaterDensity;
	public float scatteringStrength;
	public float blurDistance = 40;
	public int blurSize = 8;
	public float blurStrength = 5;
	public Color underwaterNearCol;
	public Color underwaterFarCol;
	public Vector3 underwaterParams;
	public Texture2D blueNoise;

	Material waterMaterial;

	MeshFilter filter;
	GenTest planetGen;


	void Start()
	{
		SphereMesh sphereMesh = new SphereMesh(resolution);
		Mesh mesh = new Mesh();
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		mesh.SetVertices(sphereMesh.Vertices);
		mesh.SetTriangles(sphereMesh.Triangles, 0, true);
		mesh.RecalculateNormals();

		filter = GetComponentInChildren<MeshFilter>();
		filter.mesh = mesh;
		waterMaterial = GetComponentInChildren<MeshRenderer>().material;
		planetGen = FindObjectOfType<GenTest>();


	}


	void Update()
	{
		filter.transform.localScale = Vector3.one * radius;

		waterMaterial.SetTexture("waveNormalA", waveNormalA);
		waterMaterial.SetTexture("waveNormalB", waveNormalB);
		waterMaterial.SetTexture("foamNoiseTex", foamNoise);

		waterMaterial.SetFloat("edgeFade", edgeFade);
		waterMaterial.SetColor("deepCol", deepCol);
		waterMaterial.SetColor("shallowCol", shallowCol);
		waterMaterial.SetFloat("colDepthFactor", colDepthFactor);
		waterMaterial.SetFloat("smoothness", smoothness);

		waterMaterial.SetFloat("alphaFresnelPow", alphaFresnelPow);
		waterMaterial.SetVector("params", testParams);
		waterMaterial.SetTexture("DensityTex", FindObjectOfType<GenTest>().processedDensityTexture);
		waterMaterial.SetVector("dirToSun", EffectManager.DirToSun);
		waterMaterial.SetFloat("planetBoundsSize", planetGen.boundsSize);
	}

	public void SetUnderwaterProperties(Material underwaterMaterial)
	{
		underwaterMaterial.SetVector("oceanCentre", transform.position);
		underwaterMaterial.SetFloat("oceanRadius", radius);

		underwaterMaterial.SetFloat("maxVisibility", maxVisibility);
		underwaterMaterial.SetFloat("density", underwaterDensity);
		underwaterMaterial.SetFloat("blurDistance", blurDistance);
		underwaterMaterial.SetColor("underwaterNearCol", underwaterNearCol);
		underwaterMaterial.SetColor("underwaterFarCol", underwaterFarCol);
		underwaterMaterial.SetTexture("_BlueNoise", blueNoise);
		underwaterMaterial.SetVector("params", underwaterParams);
	}
}
