using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightThrower : MonoBehaviour
{

	public StickyLight lightPrefab;
	public Transform spawnPoint;
	Rigidbody rb;

	FirstPersonController controller;
	Terraformer terraformer;

	void Start()
	{
		controller = GetComponent<FirstPersonController>();
		rb = GetComponent<Rigidbody>();
		terraformer = FindObjectOfType<Terraformer>();
	}


	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Q))
		{
			var l = Instantiate(lightPrefab, spawnPoint.position, spawnPoint.rotation);
			l.Init(rb.velocity, controller.gravity, terraformer);
		}
	}
}
