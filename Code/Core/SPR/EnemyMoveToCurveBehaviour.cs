using System.Linq;
using DG.Tweening;
using Dreamteck.Splines;
using Library;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public class EnemyMoveToCurveBehaviour : PlayableBehaviour
{
	public Enemy Enemy;

	public Pivot Pivot;

	public Vector3 AnchorPosition;

	public int Direction;

	public bool UsePositionToAnchor;

	public bool UseSharedAnchorPosition;

	public SplineComputer Spline;

	public Ease Ease;

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		if (UsePositionToAnchor)
		{
			playable.GetGraph().GetResolver().CastObject<PlayableDirector>()
				.playableAsset.outputs.FirstOrDefault((PlayableBinding x) => x.sourceObject is EnemyMoveToTrack).sourceObject.CastObject<EnemyMoveToTrack>().AnchorPosition = ((Pivot == Pivot.Self) ? Enemy.VisualBottom : Game.Instance.Player.mover.VisualBottom);
		}
		if (!UseSharedAnchorPosition)
		{
			AnchorPosition = ((Pivot == Pivot.Self) ? Enemy.VisualBottom : Game.Instance.Player.mover.VisualBottom);
			Direction = Enemy.FacingDirection.GetSignX();
		}
		if (!BasicExtension.IsNull(Spline.transform.parent))
		{
			Spline.transform.SetParent(null);
		}
		Spline.transform.position = AnchorPosition;
		Spline.transform.localScale = new Vector3(Direction, 1f, 1f);
		SplineFollower follower = Enemy.GetComponent<SplineFollower>();
		follower.spline = Spline;
		follower.DOFollow(playable.GetDuration()).SetEase(Ease).OnComplete(delegate
		{
			Enemy.transform.position = Spline.GetPoints().Last().position;
			follower.spline = null;
		});
	}
}
