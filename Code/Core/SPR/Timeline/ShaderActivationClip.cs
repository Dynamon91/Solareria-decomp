using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class ShaderActivationClip : PlayableAsset
{
	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		return ScriptPlayable<ShaderActivationBehaviour>.Create(graph);
	}
}
