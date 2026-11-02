using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;

namespace SPR;

public class TorchController : MonoBehaviour
{
	[SerializeField]
	private VisualEffect flame;

	[SerializeField]
	private Light2D light;

	[SerializeField]
	private AnimationCurve rateCurve;

	[SerializeField]
	private AnimationCurve alphaCurve;

	private float initialIntensity;

	private float maxTime;

	private float timer;

	private int sign;

	public void SetActive(bool value)
	{
		if (value)
		{
			flame.Play();
			light.intensity = 0f;
			timer = maxTime;
			sign = -1;
		}
		else
		{
			flame.Stop();
			light.intensity = initialIntensity;
			timer = 0f;
			sign = 1;
		}
	}

	private void Start()
	{
		maxTime = rateCurve[rateCurve.length - 1].time;
		initialIntensity = light.intensity;
	}

	private void Update()
	{
		float num = ((sign == 1) ? maxTime : 0f);
		if ((num - timer) * (float)sign > 0f)
		{
			timer += Time.deltaTime * (float)sign;
			if ((num - timer) * (float)sign <= 0f)
			{
				timer = num;
				sign = 0;
			}
			float num2 = rateCurve.Evaluate(timer);
			flame.playRate = num2;
			flame.SetFloat("Spawn Rate", 1f * ((sign == 1) ? num2 : 1f));
			flame.SetFloat("Lifetime", 3f / ((sign == 1) ? maxTime : 1f));
			float num3 = alphaCurve.Evaluate(timer);
			flame.SetFloat("Update Alpha", num3);
			light.intensity = initialIntensity * num3;
		}
	}
}
