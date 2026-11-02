using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackColor(0f, 1f, 0f)]
[TrackClipType(typeof(NarrationClip))]
public class NarrationTrack : TrackAsset
{
	protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
	{
		ScriptPlayable<NarrationBehaviour> scriptPlayable = ScriptPlayable<NarrationBehaviour>.Create(graph);
		NarrationBehaviour behaviour = scriptPlayable.GetBehaviour();
		NarrationClip narrationClip = (NarrationClip)clip.asset;
		behaviour.FadeDuration = narrationClip.fadeDuration;
		behaviour.LocaleKey = narrationClip.localeKey;
		return scriptPlayable;
	}
}
