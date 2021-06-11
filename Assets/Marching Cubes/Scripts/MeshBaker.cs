using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;

public struct MeshBaker : IJob
{
	int meshID;

	public MeshBaker(int meshID)
	{
		this.meshID = meshID;
	}

	public void Execute()
	{
		Physics.BakeMesh(meshID, convex: false);
	}
}
