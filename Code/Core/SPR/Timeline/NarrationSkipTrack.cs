using Library;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackColor(0f, 1f, 1f)]
[TrackClipType(typeof(NarrationSkipClip))]
[TrackBindingType(typeof(CutsceneProducer))]
public class NarrationSkipTrack : TrackAsset
{
	protected override Playable CreatePlayable(PlayableGraph graph, GameObject gameObject, TimelineClip clip)
	{
		clip.asset.CastObject<NarrationSkipClip>().SetClip(clip);
		return base.CreatePlayable(graph, gameObject, clip);
	}
}
