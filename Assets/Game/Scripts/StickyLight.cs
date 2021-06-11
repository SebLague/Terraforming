using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickyLight : MonoBehaviour
{

	public LayerMask collisionMask;
	public Material material;
	public Transform lightT;

	public float initialSpeed = 10;

	public Vector3 velocity;
	float gravity;

	bool stuck;



	public void Init(Vector3 inheritedVelocity, float gravity, Terraformer terraformer)
	{
		this.gravity = gravity;
		velocity = inheritedVelocity + transform.forward * initialSpeed;
		terraformer.onTerrainModified += OnTerrainModified;
		//velocity = transform.forward * initialSpeed;
		//Debug.Log("Init: " + velocity);
		//material.SetColor("_EmissionColor", Color.black);
	}

	void OnTerrainModified()
	{
		if (!Physics.CheckSphere(transform.position, 0.1f, collisionMask))
		{
			stuck = false;
		}
	}


	void Update()
	{

		if (stuck)
		{
			return;
		}

		Vector3 accelDueToGravity = -transform.position.normalized * gravity;
		velocity += accelDueToGravity * Time.deltaTime;
		Vector3 moveAmount = velocity * Time.deltaTime;

		float moveDst = moveAmount.magnitude;
		Vector3 moveDir = moveAmount / moveDst;


		Ray ray = new Ray(transform.position, moveDir);
		RaycastHit hitInfo;
		if (Physics.SphereCast(ray, 0.05f, out hitInfo, moveDst, collisionMask))
		{
			transform.position = hitInfo.point;

			lightT.position = hitInfo.point + hitInfo.normal * 0.5f;
			lightT.forward = hitInfo.normal;
			//Debug.DrawRay(hitInfo.point, hitInfo.normal, Color.red, 100);


			stuck = true;
			velocity = Vector3.zero;
			//material.SetColor("_EmissionColor", Color.white);
		}
		else
		{
			transform.position += moveAmount;
		}



	}


}
