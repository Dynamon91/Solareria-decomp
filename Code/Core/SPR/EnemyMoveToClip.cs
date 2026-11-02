using DG.Tweening;
using Library;
using SPR.Timeline;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public class EnemyMoveToClip : EnemyMoveToClipBase
{
	public ExposedReference<Transform> end;

	public bool useRange;

	public DistanceRange distanceRange;

	public float maxMoveRange;

	public Ease ease = Ease.Linear;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<EnemyMoveToBehaviour> scriptPlayable = ScriptPlayable<EnemyMoveToBehaviour>.Create(graph);
		EnemyMoveToBehaviour behaviour = scriptPlayable.GetBehaviour();
		Transform parent = owner.transform.parent.parent;
		behaviour.Enemy = parent.GetComponent<Enemy>();
		behaviour.Pivot = pivot;
		behaviour.Position = end.Resolve(graph.GetResolver());
		behaviour.UseRange = useRange;
		behaviour.DistanceRange = distanceRange;
		behaviour.MaxMoveRange = maxMoveRange;
		behaviour.UsePositionToAnchor = usePositionToAnchor;
		behaviour.UseSharedAnchorPosition = useAnchorPosition;
		behaviour.KeepTrackAnchor = keepTrackAnchor;
		behaviour.UseCurrentDirection = useCurrentDirection;
		behaviour.LandingGround = landingGround;
		behaviour.Ease = ease;
		if (useAnchorPosition)
		{
			behaviour.AnchorPosition = anchorPosition;
			behaviour.Direction = direction.GetSignX();
		}
		return scriptPlayable;
	}
}
