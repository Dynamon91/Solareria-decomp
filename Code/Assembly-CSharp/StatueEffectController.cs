using System.Collections.Generic;
using SPR.Library;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class StatueEffectController : MonoBehaviour
{
	[SerializeField]
	private AnimationCurve animationCurve;

	[SerializeField]
	private List<Light2D> lights;

	[SerializeField]
	private ParticleSystem ember;

	[SerializeField]
	private float speed;

	private bool isActive;

	private float t;

	private float target;

	private void Start()
	{
		SetActive(value: false);
	}

	private void FixedUpdate()
	{
		if (Game.Instance.InputManager.IsCurrentControlTarget(Game.Instance.Player))
		{
			bool flag = PhysicsUtility.IsPlayerStay(base.transform.position, base.transform.localScale, includingDefaultLayer: true);
			if (!isActive && flag)
			{
				SetActive(value: true);
			}
			else if (isActive && !flag)
			{
				SetActive(value: false);
			}
		}
	}

	private void Update()
	{
		t = Mathf.MoveTowards(t, target, speed * Time.deltaTime);
		float a = animationCurve.Evaluate(t);
		foreach (Light2D light in lights)
		{
			Color color = light.color;
			light.color = new Color(color.r, color.g, color.b, a);
		}
	}

	private void SetActive(bool value)
	{
		isActive = value;
		target = (value ? 1 : 0);
		if (value)
		{
			ember.Play(withChildren: false);
		}
		else
		{
			ember.Stop(withChildren: false, ParticleSystemStopBehavior.StopEmitting);
		}
	}
}
