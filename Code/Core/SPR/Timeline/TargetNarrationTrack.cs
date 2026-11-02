using SPR.UI;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackColor(0f, 1f, 0.6f)]
[TrackClipType(typeof(NarrationClip))]
[TrackBindingType(typeof(NarrationBoard))]
public class TargetNarrationTrack : TrackAsset
{
	protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
	{
		ScriptPlayable<TargetNarrationBehaviour> scriptPlayable = ScriptPlayable<TargetNarrationBehaviour>.Create(graph);
		TargetNarrationBehaviour behaviour = scriptPlayable.GetBehaviour();
		NarrationClip narrationClip = (NarrationClip)clip.asset;
		behaviour.FadeDuration = narrationClip.fadeDuration;
		behaviour.LocaleKey = narrationClip.localeKey;
		return scriptPlayable;
	}
}
