using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class EnemyDynamicAttackClip : EnemyAttackClipBase
{
	public Zone zone;

	public Direction direction;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<EnemyDynamicAttackBehaviour> scriptPlayable = ScriptPlayable<EnemyDynamicAttackBehaviour>.Create(graph);
		EnemyDynamicAttackBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.Key = key;
		behaviour.Zone = zone;
		behaviour.Direction = direction;
		return scriptPlayable;
	}
}
