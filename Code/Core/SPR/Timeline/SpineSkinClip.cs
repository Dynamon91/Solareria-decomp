using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class SpineSkinClip : PlayableAsset
{
	public string skinName;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<SpineSkinBehaviour> scriptPlayable = ScriptPlayable<SpineSkinBehaviour>.Create(graph);
		scriptPlayable.GetBehaviour().SkinName = skinName;
		return scriptPlayable;
	}
}
