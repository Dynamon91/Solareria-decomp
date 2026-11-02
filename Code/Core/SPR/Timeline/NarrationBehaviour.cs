using Library;
using SPR.UI;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class NarrationBehaviour : PlayableBehaviour
{
	public float FadeDuration = 1f;

	public string LocaleKey = string.Empty;

	public bool AutoFade;

	private static NarrationBoard NarrationBoard => Game.Instance.UIManager.FieldCanvas.NarrationBoard;

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		if (Application.isPlaying && !LocaleKey.IsNullOrEmpty() && !BasicExtension.IsNull(NarrationBoard))
		{
			ScenarioData scenarioData = DataManager.Instance.GetScenarioData(LocaleKey);
			NarrationBoard.SetScenario(scenarioData);
			NarrationBoard.SetVoice(scenarioData.Actor, scenarioData.VoiceKey);
			if (AutoFade)
			{
				NarrationBoard.SetActiveDuration((float)playable.GetDuration(), FadeDuration);
			}
			else
			{
				NarrationBoard.SetActiveContinue(FadeDuration);
			}
		}
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		if (Application.isPlaying && !LocaleKey.IsNullOrEmpty() && !BasicExtension.IsNull(NarrationBoard))
		{
			if (AutoFade)
			{
				NarrationBoard.Clear();
				NarrationBoard.DisableNow();
			}
			else
			{
				NarrationBoard.FadeOutNow(FadeDuration, disableControlGuide: true);
			}
		}
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		if (Application.isPlaying && !LocaleKey.IsNullOrEmpty())
		{
			BasicExtension.IsNull(NarrationBoard);
		}
	}
}
