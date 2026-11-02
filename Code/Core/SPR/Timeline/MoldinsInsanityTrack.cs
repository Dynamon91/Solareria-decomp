using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackClipType(typeof(MoldinsInsanityClip))]
public class MoldinsInsanityTrack : TrackAsset
{
	public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
	{
		return ScriptPlayable<MoldinsInsanityMixerBehaviour>.Create(graph, inputCount);
	}
}
