using Library;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Timeline;

public class EnemyMoveBehaviour : PlayableBehaviour
{
	public Enemy Enemy;

	public bool IgnorePlayer;

	public bool KeepTrackPlayerPos;

	public bool RandomDirection;

	public bool MoveForward;

	public bool UseRange;

	public float Distance;

	public DistanceRange DistanceRange;

	public float Speed;

	private PlayerMover player;

	private Vector2 playerLastPosition;

	private int elapsedFrame;

	public override void OnBehaviourPlay(Playable playable, FrameData info)
	{
		player = Game.Instance.Player.mover;
		elapsedFrame = 0;
		MoveForward = (RandomDirection ? CryptoRandom.RandomBool() : MoveForward);
		SetPlayerLastPosition(playable.GetDuration());
	}

	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		if (IgnorePlayer)
		{
			Enemy.MoveX(Speed, info.deltaTime, MoveForward);
			return;
		}
		if (KeepTrackPlayerPos)
		{
			SetPlayerLastPosition(playable.GetDuration());
		}
		float velocityX = Mathf.Clamp(Vector2.Distance(playerLastPosition, Enemy.VisualBottom) / info.deltaTime, 0f, Speed);
		Vector2 normalized = (playerLastPosition - Enemy.VisualBottom).normalized;
		int num = (MoveForward ? Enemy.FacingDirection.GetSignX() : (-Enemy.FacingDirection.GetSignX()));
		if (MoveForward && num == normalized.x.GetSign())
		{
			Enemy.MoveX(velocityX, info.deltaTime, MoveForward);
		}
		elapsedFrame++;
	}

	private void SetPlayerLastPosition(double duration)
	{
		float num3;
		if (UseRange)
		{
			float num = Mathf.Abs(DistanceRange.to - DistanceRange.from) / (float)Util.SecondsToFrame(duration);
			int num2 = ((DistanceRange.from < DistanceRange.to) ? 1 : (-1));
			num3 = DistanceRange.from + (float)num2 * (num * (float)elapsedFrame);
		}
		else
		{
			num3 = Distance;
		}
		playerLastPosition = player.VisualBottom;
		playerLastPosition.x += num3 * (float)(-Enemy.FacingDirection.GetSignX());
	}
}
