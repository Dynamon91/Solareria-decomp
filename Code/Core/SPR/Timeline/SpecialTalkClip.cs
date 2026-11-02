using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class SpecialTalkClip : PlayableAsset
{
	public float fadeDuration = 0.6f;

	public string scenarioId = string.Empty;

	public int scriptNum = 1;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<SpecialTalkBehaviour> scriptPlayable = ScriptPlayable<SpecialTalkBehaviour>.Create(graph);
		SpecialTalkBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.FadeDuration = fadeDuration;
		behaviour.ScenarioId = scenarioId;
		behaviour.ScriptNum = scriptNum;
		return scriptPlayable;
	}
}
