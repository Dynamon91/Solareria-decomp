using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackColor(0f, 0.8f, 0.8f)]
[TrackClipType(typeof(SpecialTalkClip))]
public class SpecialTalkTrack : TrackAsset
{
	protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
	{
		ScriptPlayable<SpecialTalkBehaviour> scriptPlayable = ScriptPlayable<SpecialTalkBehaviour>.Create(graph);
		SpecialTalkBehaviour behaviour = scriptPlayable.GetBehaviour();
		SpecialTalkClip specialTalkClip = (SpecialTalkClip)clip.asset;
		behaviour.FadeDuration = specialTalkClip.fadeDuration;
		behaviour.ScenarioId = specialTalkClip.scenarioId;
		behaviour.ScriptNum = specialTalkClip.scriptNum;
		return scriptPlayable;
	}
}
