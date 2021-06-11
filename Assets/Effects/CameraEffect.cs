using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraEffect : MonoBehaviour
{
	EffectManager effectManager;

	void OnRenderImage(RenderTexture source, RenderTexture target)
	{
		Init();

		if (effectManager != null)
		{
			effectManager.HandleEffects(source, target);
		}
		else
		{
			Graphics.Blit(source, target);
		}
	}

	void Init()
	{
		if (effectManager == null)
		{
			effectManager = FindObjectOfType<EffectManager>();
		}
	}
}