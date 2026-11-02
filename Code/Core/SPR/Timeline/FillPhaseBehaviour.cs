using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class FillPhaseBehaviour : PlayableBehaviour
{
	public int VibrationAmount;

	public float MaxFillPhase;

	private Material material;

	private static readonly int fillPhase = Shader.PropertyToID("_FillPhase");

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		if (!(material == null))
		{
			material.SetFloat(fillPhase, 0f);
		}
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		if (!(material == null))
		{
			material.SetFloat(fillPhase, 0f);
		}
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		Renderer renderer = playerData as Renderer;
		if (!(renderer == null))
		{
			if (material == null)
			{
				material = renderer.sharedMaterial;
			}
			float num = (float)playable.GetDuration() / (float)VibrationAmount;
			float num2 = (float)playable.GetTime() % num;
			float num3 = 0f - Mathf.Abs(2f * num2 / num - 1f) + 1f;
			material.SetFloat(fillPhase, num3 * MaxFillPhase);
		}
	}
}
