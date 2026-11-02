using Library;
using OccaSoftware.RadialBlur.Runtime;
using SPR.Library;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class RadialBlurMixerBehaviour : PlayableBehaviour
{
	public RadialBlurPostProcess RadialBlur;

	private static int behaviourCount;

	private static bool shouldInitializeTransform = true;

	private static float initialIntensity;

	private static Vector2 initialCenter;

	private static float initialDelay;

	private static int initialNumberOfSamples;

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		if (RadialBlur == null)
		{
			return;
		}
		InitializeIfNecessary(RadialBlur);
		float num = 0f;
		Vector2 zero = Vector2.zero;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		int inputCount = playable.GetInputCount();
		for (int i = 0; i < inputCount; i++)
		{
			float inputWeight = playable.GetInputWeight(i);
			RadialBlurBehaviour behaviour = ((ScriptPlayable<RadialBlurBehaviour>)playable.GetInput(i)).GetBehaviour();
			num += behaviour.Intensity * inputWeight;
			if (BasicExtension.IsNull(behaviour.Target))
			{
				zero += behaviour.Center * inputWeight;
			}
			else
			{
				Vector2 vector = -GraphicHelper.GetNormalizedScreenPoint(behaviour.Target.position) * 2f + Vector2.one;
				zero += vector * inputWeight;
			}
			num2 += behaviour.Delay * inputWeight;
			num3 += (float)behaviour.NumberOfSamples * inputWeight;
			num4 += inputWeight;
		}
		float intensity = Mathf.Lerp(initialIntensity, num, num4);
		Vector2 center = Vector2.Lerp(initialCenter, zero, num4);
		float delay = Mathf.Lerp(initialDelay, num2, num4);
		int numberOfSamples = Mathf.RoundToInt(Mathf.Lerp(initialNumberOfSamples, num3, num4));
		SetRadialBlurParameter(RadialBlur, intensity, center, delay, numberOfSamples);
	}

	public override void OnPlayableDestroy(Playable playable)
	{
		if (!(RadialBlur == null))
		{
			behaviourCount--;
			if (behaviourCount <= 0)
			{
				SetRadialBlurParameter(RadialBlur, initialIntensity, initialCenter, initialDelay, initialNumberOfSamples);
				shouldInitializeTransform = true;
				behaviourCount = 0;
			}
		}
	}

	public override void OnPlayableCreate(Playable playable)
	{
		behaviourCount++;
	}

	public override void OnGraphStop(Playable playable)
	{
		if (!(RadialBlur == null))
		{
			SetRadialBlurParameter(RadialBlur, 0f, Vector2.zero, 0f, 4);
		}
	}

	private void InitializeIfNecessary(RadialBlurPostProcess radialBlur)
	{
		if (shouldInitializeTransform)
		{
			initialIntensity = radialBlur.GetIntensity();
			initialCenter = radialBlur.GetCenter();
			initialDelay = radialBlur.GetDelay();
			initialNumberOfSamples = radialBlur.GetSampleCount();
			shouldInitializeTransform = false;
		}
	}

	private void SetRadialBlurParameter(RadialBlurPostProcess radialBlur, float intensity, Vector2 center, float delay, int numberOfSamples)
	{
		radialBlur.active = intensity > 0f;
		radialBlur.SetIntensity(intensity);
		radialBlur.SetCenter(center);
		radialBlur.SetDelay(delay);
		radialBlur.SetSampleCount(numberOfSamples);
	}
}
