using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public class EnemyParameterActionClip : EnemyActionClipBase
{
	[SerializeField]
	private Parameter[] parameters;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<EnemyParameterActionBehaviour> scriptPlayable = ScriptPlayable<EnemyParameterActionBehaviour>.Create(graph);
		EnemyParameterActionBehaviour behaviour = scriptPlayable.GetBehaviour();
		Transform parent = owner.transform.parent.parent;
		behaviour.Enemy = parent.GetComponent<Enemy>();
		behaviour.Method = method;
		behaviour.Parameters = parameters;
		return scriptPlayable;
	}
}
