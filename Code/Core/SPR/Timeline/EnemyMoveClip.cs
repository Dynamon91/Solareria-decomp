using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class EnemyMoveClip : PlayableAsset
{
	[Tooltip("플레이어 무시 여부")]
	public bool ignorePlayer;

	[Tooltip("플레이어 위치 실시간 추적")]
	public bool keepTrackPlayerPos;

	public bool randomDirection;

	public bool moveForward = true;

	public bool useRange;

	public float distance;

	public DistanceRange distanceRange;

	public float speed;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<EnemyMoveBehaviour> scriptPlayable = ScriptPlayable<EnemyMoveBehaviour>.Create(graph);
		EnemyMoveBehaviour behaviour = scriptPlayable.GetBehaviour();
		behaviour.Enemy = owner.transform.parent.parent.GetComponent<Enemy>();
		behaviour.IgnorePlayer = ignorePlayer;
		behaviour.KeepTrackPlayerPos = keepTrackPlayerPos;
		behaviour.RandomDirection = randomDirection;
		behaviour.MoveForward = moveForward;
		behaviour.Distance = distance;
		behaviour.UseRange = useRange;
		behaviour.DistanceRange = distanceRange;
		behaviour.Speed = speed;
		return scriptPlayable;
	}
}
