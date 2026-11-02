using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Timeline;

[TrackClipType(typeof(ShaderClip))]
[TrackBindingType(typeof(Material))]
public class ShaderTrack : TrackAsset
{
	public string shaderPropertyName;

	public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
	{
		ScriptPlayable<ShaderMixer> scriptPlayable = ScriptPlayable<ShaderMixer>.Create(graph, inputCount);
		scriptPlayable.GetBehaviour().ShaderPropertyName = shaderPropertyName;
		return scriptPlayable;
	}
}
