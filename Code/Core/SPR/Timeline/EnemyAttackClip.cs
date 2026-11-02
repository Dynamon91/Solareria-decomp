using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class EnemyAttackClip : EnemyAttackClipBase
{
	public List<Zone> zones;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<EnemyAttackBehaviour> scriptPlayable = ScriptPlayable<EnemyAttackBehaviour>.Create(graph);
		EnemyAttackBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.Enemy = owner.transform.parent.parent.GetComponent<Enemy>();
		behaviour.Key = key;
		behaviour.Zones = zones;
		behaviour.Interval = interval;
		behaviour.IgnoreStun = ignoreStun;
		behaviour.SplitDirection = splitDirection;
		if (isShared)
		{
			behaviour.SharedDamageableList = SharedDamageableList;
		}
		return scriptPlayable;
	}
}
