using Library;
using SPR.UI;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class TargetNarrationBehaviour : PlayableBehaviour
{
	public float FadeDuration = 1f;

	public string LocaleKey = string.Empty;

	private NarrationBoard narrationBoard;

	private bool isInitialized;

	private void Initialize(object data)
	{
		if (!isInitialized)
		{
			narrationBoard = data as NarrationBoard;
			if (BasicExtension.IsNull(narrationBoard))
			{
				DebugLog.GeneralLogError("Target Narration Track에 Narration Board가 할당되지 않았습니다!");
			}
			else
			{
				isInitialized = true;
			}
		}
	}

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		if (Application.isPlaying && !LocaleKey.IsNullOrEmpty())
		{
			if (!isInitialized)
			{
				Initialize(info.output.GetUserData());
			}
			if (!BasicExtension.IsNull(narrationBoard))
			{
				ScenarioData scenarioData = DataManager.Instance.GetScenarioData(LocaleKey);
				narrationBoard.SetScenario(scenarioData);
				narrationBoard.SetVoice(scenarioData.Actor, scenarioData.VoiceKey);
				narrationBoard.SetActiveDuration((float)playable.GetDuration(), FadeDuration);
			}
		}
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		if (Application.isPlaying && !LocaleKey.IsNullOrEmpty())
		{
			if (!isInitialized)
			{
				Initialize(info.output.GetUserData());
			}
			if (!BasicExtension.IsNull(narrationBoard))
			{
				narrationBoard.Clear();
				narrationBoard.DisableNow();
			}
		}
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		if (Application.isPlaying && !LocaleKey.IsNullOrEmpty())
		{
			if (!isInitialized)
			{
				Initialize(info.output.GetUserData());
			}
			BasicExtension.IsNull(narrationBoard);
		}
	}
}
