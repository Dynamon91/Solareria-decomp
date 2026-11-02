using DG.Tweening;
using Dreamteck.Splines;
using Library;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public class EnemyMoveToCurveClip : EnemyMoveToClipBase
{
	public ExposedReference<SplineComputer> spline;

	public Ease ease = Ease.Linear;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		ScriptPlayable<EnemyMoveToCurveBehaviour> scriptPlayable = ScriptPlayable<EnemyMoveToCurveBehaviour>.Create(graph);
		EnemyMoveToCurveBehaviour behaviour = scriptPlayable.GetBehaviour();
		Transform parent = owner.transform.parent.parent;
		behaviour.Enemy = parent.GetComponent<Enemy>();
		behaviour.Pivot = pivot;
		behaviour.Spline = spline.Resolve(graph.GetResolver());
		behaviour.UsePositionToAnchor = usePositionToAnchor;
		behaviour.UseSharedAnchorPosition = useAnchorPosition;
		behaviour.Ease = ease;
		if (useAnchorPosition)
		{
			behaviour.AnchorPosition = anchorPosition;
			behaviour.Direction = direction.GetSignX();
		}
		return scriptPlayable;
	}
}
