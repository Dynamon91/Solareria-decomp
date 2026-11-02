using System.Linq;
using DG.Tweening;
using Library;
using SPR.Timeline;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public class EnemyMoveToBehaviour : PlayableBehaviour
{
	public Enemy Enemy;

	public Pivot Pivot;

	public Vector3 AnchorPosition;

	public int Direction;

	public bool UseRange;

	public DistanceRange DistanceRange;

	public float MaxMoveRange;

	public bool KeepTrackAnchor;

	public bool UsePositionToAnchor;

	public bool UseSharedAnchorPosition;

	public bool UseCurrentDirection;

	public bool LandingGround;

	public Transform Position;

	public Ease Ease;

	private float distance;

	private Vector2 directionToAnchor;

	private int elapsedFrame;

	private (float left, float right) moveRange;

	private Tween tween;

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		elapsedFrame = 0;
		if (UseCurrentDirection)
		{
			Direction = Enemy.FacingDirection.GetSignX();
		}
		if (MaxMoveRange != 0f)
		{
			moveRange.left = Enemy.transform.position.x - MaxMoveRange;
			moveRange.right = Enemy.transform.position.x + MaxMoveRange;
		}
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
		Vector3 vector = new Vector3(AnchorPosition.x + Position.localPosition.x * (float)Direction, AnchorPosition.y + Position.localPosition.y);
		if (LandingGround)
		{
			vector.y = Enemy.GroundPosition.y;
		}
		if (UseRange)
		{
			UpdateDistanceRange(playable.GetDuration());
			directionToAnchor = (Enemy.transform.position - AnchorPosition).normalized;
			tween = DoRangeMove(vector, playable.GetDuration());
			return;
		}
		if (MaxMoveRange != 0f)
		{
			vector.x = Mathf.Clamp(vector.x, moveRange.left, moveRange.right);
		}
		Enemy.transform.DOMove(vector, (float)playable.GetDuration()).SetEase(Ease);
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		if (UseRange)
		{
			if (KeepTrackAnchor)
			{
				AnchorPosition = ((Pivot == Pivot.Self) ? Enemy.VisualBottom : Game.Instance.Player.mover.VisualBottom);
				directionToAnchor = (Enemy.transform.position - AnchorPosition).normalized;
			}
			UpdateDistanceRange(playable.GetDuration());
			tween?.Kill();
			Vector3 position = new Vector3(AnchorPosition.x + Position.localPosition.x * (float)Direction, AnchorPosition.y + Position.localPosition.y);
			tween = DoRangeMove(position, playable.GetDuration());
			tween?.Goto((float)(playable.GetTime() / playable.GetDuration()));
			elapsedFrame++;
		}
	}

	private void UpdateDistanceRange(double duration)
	{
		float num = Mathf.Abs(DistanceRange.to - DistanceRange.from) / (float)Util.SecondsToFrame(duration);
		int num2 = ((DistanceRange.from < DistanceRange.to) ? 1 : (-1));
		distance = DistanceRange.from + (float)num2 * (num * (float)elapsedFrame);
	}

	private Tween DoRangeMove(Vector3 position, double duration)
	{
		Vector3 endValue = position + new Vector3((float)directionToAnchor.x.GetSign() * distance, 0f);
		Vector3 position2 = Enemy.transform.position;
		if ((Direction == -1 && (position2.x < endValue.x || (MaxMoveRange != 0f && position2.x < moveRange.left))) || (Direction == 1 && (position2.x > endValue.x || (MaxMoveRange != 0f && position2.x > moveRange.right))))
		{
			return DOTween.To(() => Enemy.transform.position, delegate(Vector3 x)
			{
				Enemy.transform.position = x;
			}, new Vector3(Enemy.transform.position.x, endValue.y), (float)duration).SetUpdate(UpdateType.Fixed).SetEase(Ease);
		}
		return DOTween.To(() => Enemy.transform.position, delegate(Vector3 x)
		{
			Enemy.transform.position = x;
		}, endValue, (float)duration).SetUpdate(UpdateType.Fixed).SetEase(Ease);
	}
}
