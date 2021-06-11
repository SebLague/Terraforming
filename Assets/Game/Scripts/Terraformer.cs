using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Terraformer : MonoBehaviour
{

	public event System.Action onTerrainModified;

	public LayerMask terrainMask;

	public float terraformRadius = 5;
	public float terraformSpeedNear = 0.1f;
	public float terraformSpeedFar = 0.25f;


	Transform cam;
	GenTest genTest;
	bool hasHit;
	Vector3 hitPoint;
	FirstPersonController firstPersonController;

	bool isTerraforming;
	Vector3 lastTerraformPointLocal;

	void Start()
	{
		genTest = FindObjectOfType<GenTest>();
		cam = Camera.main.transform;
		firstPersonController = FindObjectOfType<FirstPersonController>();
	}

	void Update()
	{
		RaycastHit hit;
		hasHit = false;

		bool wasTerraformingLastFrame = isTerraforming;
		isTerraforming = false;

		int numIterations = 5;
		bool rayHitTerrain = false;



		for (int i = 0; i < numIterations; i++)
		{
			float rayRadius = terraformRadius * Mathf.Lerp(0.01f, 1, i / (numIterations - 1f));
			if (Physics.SphereCast(cam.position, rayRadius, cam.forward, out hit, 1000, terrainMask))
			{
				lastTerraformPointLocal = MathUtility.WorldToLocalVector(cam.rotation, hit.point);
				Terraform(hit.point);
				rayHitTerrain = true;
				break;
			}
		}


		if (!rayHitTerrain && wasTerraformingLastFrame)
		{
			Vector3 terraformPoint = MathUtility.LocalToWorldVector(cam.rotation, lastTerraformPointLocal);
			Terraform(terraformPoint);
		}

	}

	void Terraform(Vector3 terraformPoint)
	{
		//Debug.DrawLine(cam.position, point, Color.green);
		hasHit = true;
		hitPoint = terraformPoint;

		const float dstNear = 10;
		const float dstFar = 60;

		float dstFromCam = (terraformPoint - cam.position).magnitude;
		float weight01 = Mathf.InverseLerp(dstNear, dstFar, dstFromCam);
		float weight = Mathf.Lerp(terraformSpeedNear, terraformSpeedFar, weight01);

		// Add terrain
		if (Input.GetMouseButton(0))
		{
			isTerraforming = true;
			genTest.Terraform(terraformPoint, -weight, terraformRadius);
			firstPersonController.NotifyTerrainChanged(terraformPoint, terraformRadius);
		}
		// Subtract terrain
		else if (Input.GetMouseButton(1))
		{
			isTerraforming = true;
			genTest.Terraform(terraformPoint, weight, terraformRadius);
		}

		if (isTerraforming)
		{
			onTerrainModified?.Invoke();
		}
	}

	void OnDrawGizmos()
	{
		if (hasHit)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawSphere(hitPoint, 0.25f);
		}
	}
}
