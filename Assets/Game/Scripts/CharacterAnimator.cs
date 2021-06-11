using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
	Animator animator;
	FirstPersonController controller;

	float speedPercent;

	void Start()
	{
		animator = GetComponentInChildren<Animator>();
		controller = GetComponent<FirstPersonController>();
	}


	void Update()
	{
		var state = controller.currentMoveState;

		float targetSpeedPercent = 0;
		if (state == FirstPersonController.MoveState.Walk)
		{
			targetSpeedPercent = 0.5f;
		}
		else if (state == FirstPersonController.MoveState.Run)
		{
			targetSpeedPercent = 1;
		}
		else if (state == FirstPersonController.MoveState.Swim)
		{
			targetSpeedPercent = 0.5f;
		}

		speedPercent = Mathf.Lerp(speedPercent, targetSpeedPercent, Time.deltaTime * 3);

		animator.SetFloat("Speed Percent", speedPercent);
		animator.SetBool("Air", !controller.grounded);
	}
}
