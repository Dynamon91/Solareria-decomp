using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackClipType(typeof(ShockWaveClip))]
[TrackBindingType(typeof(Transform))]
public class ShockWave2Track : TrackAsset
{
	public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
	{
		return ScriptPlayable<ShockWave2MixerBehaviour>.Create(graph, inputCount);
	}
}
