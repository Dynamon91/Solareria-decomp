using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class EnemyFlipClip : PlayableAsset
{
	[Tooltip("true: 플레이어가 있는 방향으로 전환\nfalse: 바라보는 방향 반대로 전환")]
	public bool lookPlayer = true;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<EnemyFlipBehaviour> scriptPlayable = ScriptPlayable<EnemyFlipBehaviour>.Create(graph);
		scriptPlayable.GetBehaviour().LookPlayer = lookPlayer;
		return scriptPlayable;
	}
}
