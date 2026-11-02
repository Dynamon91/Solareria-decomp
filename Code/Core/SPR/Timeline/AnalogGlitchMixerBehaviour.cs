using URPGlitch.Runtime.AnalogGlitch;
using URPGlitch.Runtime.DigitalGlitch;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class AnalogGlitchMixerBehaviour : PlayableBehaviour
{
	public AnalogGlitchVolume AnalogVolume;

	public DigitalGlitchVolume DigitalVolume;

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		int inputCount = playable.GetInputCount();
		for (int i = 0; i < inputCount; i++)
		{
			float inputWeight = playable.GetInputWeight(i);
			AnalogGlitchBehaviour behaviour = ((ScriptPlayable<AnalogGlitchBehaviour>)playable.GetInput(i)).GetBehaviour();
			num += behaviour.ScanLineJitter * inputWeight;
			num2 += behaviour.VerticalJump * inputWeight;
			num3 += behaviour.HorizontalShake * inputWeight;
			num4 += behaviour.ColorDrift * inputWeight;
			num5 += behaviour.DigitalIntensity * inputWeight;
			num6 += inputWeight;
		}
		float scanLineJitter = Mathf.Lerp(0f, num, num6);
		float verticalJump = Mathf.Lerp(0f, num2, num6);
		float horizontalShake = Mathf.Lerp(0f, num3, num6);
		float colorDrift = Mathf.Lerp(0f, num4, num6);
		float digitalIntensity = Mathf.Lerp(0f, num5, num6);
		SetVolume(scanLineJitter, verticalJump, horizontalShake, colorDrift, digitalIntensity);
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		ResetMaterial();
	}

	public override void OnPlayableDestroy(Playable playable)
	{
		ResetMaterial();
	}

	private void ResetMaterial()
	{
		SetVolume(0f, 0f, 0f, 0f, 0f);
	}

	private void SetVolume(float scanLineJitter, float verticalJump, float horizontalShake, float colorDrift, float digitalIntensity)
	{
		AnalogVolume.scanLineJitter.value = scanLineJitter;
		AnalogVolume.verticalJump.value = verticalJump;
		AnalogVolume.horizontalShake.value = horizontalShake;
		AnalogVolume.colorDrift.value = colorDrift;
		DigitalVolume.intensity.value = digitalIntensity;
	}
}
