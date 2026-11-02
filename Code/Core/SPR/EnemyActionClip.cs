using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public class EnemyActionClip : EnemyActionClipBase
{
	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<EnemyActionBehaviour> scriptPlayable = ScriptPlayable<EnemyActionBehaviour>.Create(graph);
		EnemyActionBehaviour behaviour = scriptPlayable.GetBehaviour();
		Transform parent = owner.transform.parent.parent;
		behaviour.Enemy = parent.GetComponent<Enemy>();
		behaviour.Method = method;
		return scriptPlayable;
	}
}
