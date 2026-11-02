using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackClipType(typeof(ShockWaveClip))]
[TrackBindingType(typeof(Transform))]
public class ShockWave3Track : TrackAsset
{
	public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
	{
		return ScriptPlayable<ShockWave3MixerBehaviour>.Create(graph, inputCount);
	}
}
