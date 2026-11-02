using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class ShockWaveClip : PlayableAsset
{
	public float size;

	public float distance;

	public float power;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<ShockWaveBehaviour> scriptPlayable = ScriptPlayable<ShockWaveBehaviour>.Create(graph);
		ShockWaveBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.Size = size;
		behaviour.Distance = distance;
		behaviour.Power = power;
		return scriptPlayable;
	}
}
