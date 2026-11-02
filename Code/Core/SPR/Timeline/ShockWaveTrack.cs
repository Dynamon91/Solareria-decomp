using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackClipType(typeof(ShockWaveClip))]
[TrackBindingType(typeof(Transform))]
public class ShockWaveTrack : TrackAsset
{
	public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
	{
		return ScriptPlayable<ShockWaveMixerBehaviour>.Create(graph, inputCount);
	}
}
