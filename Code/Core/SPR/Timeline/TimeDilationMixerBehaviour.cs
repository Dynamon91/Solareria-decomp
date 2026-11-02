using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class TimeDilationMixerBehaviour : PlayableBehaviour
{
	private static float defaultTimeScale = 1f;

	private float myDefaultTimeScale = 1f;

	private bool first = true;

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		int inputCount = playable.GetInputCount();
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < inputCount; i++)
		{
			float inputWeight = playable.GetInputWeight(i);
			TimeDilationBehaviour behaviour = ((ScriptPlayable<TimeDilationBehaviour>)playable.GetInput(i)).GetBehaviour();
			num += inputWeight * behaviour.TimeScale;
			num2 += inputWeight;
		}
		if (num2 != 0f)
		{
			float timeScale = Mathf.Max(0.0001f, Mathf.Lerp(defaultTimeScale, num, Mathf.Clamp01(num2)));
			if (Game.GetTimeScale() == 0f)
			{
				timeScale = 0f;
			}
			Game.SetTimeScale(timeScale);
		}
	}

	public override void OnPlayableCreate(Playable playable)
	{
		float timeScale = Time.timeScale;
		myDefaultTimeScale = timeScale;
		first = Mathf.Approximately(myDefaultTimeScale, defaultTimeScale);
	}

	public override void OnPlayableDestroy(Playable playable)
	{
		if (!first && !Mathf.Approximately(myDefaultTimeScale, defaultTimeScale))
		{
			myDefaultTimeScale = defaultTimeScale;
		}
		Game.SetTimeScale(myDefaultTimeScale);
	}
}
