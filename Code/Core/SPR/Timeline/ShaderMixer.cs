using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class ShaderMixer : PlayableBehaviour
{
	public string ShaderPropertyName;

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		if (playerData is Material material)
		{
			int inputCount = playable.GetInputCount();
			float num = 0f;
			for (int i = 0; i < inputCount; i++)
			{
				float inputWeight = playable.GetInputWeight(i);
				ShaderBehaviour behaviour = ((ScriptPlayable<ShaderBehaviour>)playable.GetInput(i)).GetBehaviour();
				num += behaviour.floatValue * inputWeight;
			}
			material.SetFloat(ShaderPropertyName, num);
		}
	}
}
