using Library;
using SPR.UI.Talk;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class SpecialTalkBehaviour : PlayableBehaviour
{
	public float FadeDuration = 1f;

	public string ScenarioId = string.Empty;

	public int ScriptNum = 1;

	private static TalkCanvas TalkCanvas => Game.Instance.UIManager.TalkCanvas;

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		if (Application.isPlaying && !ScenarioId.IsNullOrEmpty() && !BasicExtension.IsNull(TalkCanvas))
		{
			TalkCanvas.ShowSpecialTalk(ScenarioId, ScriptNum, (float)playable.GetDuration(), FadeDuration);
		}
	}

	public override void OnBehaviourPause(Playable playable, FrameData info)
	{
		if (Application.isPlaying && !ScenarioId.IsNullOrEmpty() && !BasicExtension.IsNull(TalkCanvas))
		{
			TalkCanvas.HideSpecialTalk();
		}
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		if (Application.isPlaying && !ScenarioId.IsNullOrEmpty())
		{
			BasicExtension.IsNull(TalkCanvas);
		}
	}
}
