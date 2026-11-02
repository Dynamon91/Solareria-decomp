using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public class PlayerFlameClip : PlayableAsset
{
	public HeatFlame.CurveParameter curveParameter;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<PlayerFlameBehaviour> scriptPlayable = ScriptPlayable<PlayerFlameBehaviour>.Create(graph);
		PlayerFlameBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.Owner = owner;
		behaviour.CurveParameter = curveParameter;
		return scriptPlayable;
	}
}
