using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

public class Chunk
{

	public Vector3 centre;
	public float size;
	public Mesh mesh;

	public ComputeBuffer pointsBuffer;
	int numPointsPerAxis;
	public MeshFilter filter;
	MeshRenderer renderer;
	MeshCollider collider;
	public bool terra;
	public Vector3Int id;

	// Mesh processing
	Dictionary<int2, int> vertexIndexMap;
	List<Vector3> processedVertices;
	List<Vector3> processedNormals;
	List<int> processedTriangles;


	public Chunk(Vector3Int coord, Vector3 centre, float size, int numPointsPerAxis, GameObject meshHolder)
	{
		this.id = coord;
		this.centre = centre;
		this.size = size;
		this.numPointsPerAxis = numPointsPerAxis;

		mesh = new Mesh();
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

		int numPointsTotal = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
		ComputeHelper.CreateStructuredBuffer<PointData>(ref pointsBuffer, numPointsTotal);

		// Mesh rendering and collision components
		filter = meshHolder.AddComponent<MeshFilter>();
		renderer = meshHolder.AddComponent<MeshRenderer>();


		filter.mesh = mesh;
		collider = renderer.gameObject.AddComponent<MeshCollider>();

		vertexIndexMap = new Dictionary<int2, int>();
		processedVertices = new List<Vector3>();
		processedNormals = new List<Vector3>();
		processedTriangles = new List<int>();
	}

	public void CreateMesh(VertexData[] vertexData, int numVertices, bool useFlatShading)
	{

		vertexIndexMap.Clear();
		processedVertices.Clear();
		processedNormals.Clear();
		processedTriangles.Clear();

		int triangleIndex = 0;

		for (int i = 0; i < numVertices; i++)
		{
			VertexData data = vertexData[i];

			int sharedVertexIndex;
			if (!useFlatShading && vertexIndexMap.TryGetValue(data.id, out sharedVertexIndex))
			{
				processedTriangles.Add(sharedVertexIndex);
			}
			else
			{
				if (!useFlatShading)
				{
					vertexIndexMap.Add(data.id, triangleIndex);
				}
				processedVertices.Add(data.position);
				processedNormals.Add(data.normal);
				processedTriangles.Add(triangleIndex);
				triangleIndex++;
			}
		}

		collider.sharedMesh = null;

		mesh.Clear();
		mesh.SetVertices(processedVertices);
		mesh.SetTriangles(processedTriangles, 0, true);

		if (useFlatShading)
		{
			mesh.RecalculateNormals();
		}
		else
		{
			mesh.SetNormals(processedNormals);
		}

		collider.sharedMesh = mesh;
	}

	public struct PointData
	{
		public Vector3 position;
		public Vector3 normal;
		public float density;
	}

	public void AddCollider()
	{
		collider.sharedMesh = mesh;
	}

	public void SetMaterial(Material material)
	{
		renderer.material = material;
	}

	public void Release()
	{
		ComputeHelper.Release(pointsBuffer);
	}

	public void DrawBoundsGizmo(Color col)
	{
		Gizmos.color = col;
		Gizmos.DrawWireCube(centre, Vector3.one * size);
	}
}