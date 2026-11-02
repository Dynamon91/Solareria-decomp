using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class ShaderClip : PlayableAsset
{
	public float floatValue;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<ShaderBehaviour> scriptPlayable = ScriptPlayable<ShaderBehaviour>.Create(graph);
		scriptPlayable.GetBehaviour().floatValue = floatValue;
		return scriptPlayable;
	}
}
