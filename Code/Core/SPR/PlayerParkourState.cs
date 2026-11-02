using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using Library;
using UnityEngine;

namespace SPR;

public class PlayerParkourState : PlayerAsyncState
{
	private Vector2 targetPosition;

	private bool isMoving;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public PlayerParkourState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	protected override void OnEnter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.GravityFree, active: true);
		PlayerMover.ResetRelativeVelocity();
		isMoving = false;
	}

	protected override void OnFixedUpdate(float fdt)
	{
		if (isMoving && Vector3.Distance(PlayerMover.transform.position, targetPosition) > 5f)
		{
			psm.ChangeState(PlayerMover.IsOnGround ? psm.IdleState : psm.FallState);
		}
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		SoundManager.PlayOneShot("event:/SFX/Player/Move/SFX_Parkour_Grab");
		await PlayAnimationAsync("parkour_start", loop: false, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		PlayerMover.SetAnimation("parkour", loop: false);
		PlayerMover.ApplyRelativeVelocity(Direction.Up, PlayerAsyncState.pcp.ParkourSpeed, faceJump: false);
		SoundManager.PlayOneShot("event:/SFX/Player/Move/SFX_Parkour_Move");
		isMoving = true;
		await UniTask.WaitUntil(() => PlayerMover.transform.position.y >= targetPosition.y, PlayerLoopTiming.Update, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		PlayerMover.ApplyRelativeVelocity(PlayerMover.FacingDirection, PlayerAsyncState.pcp.RunSpeed, faceJump: false);
		PlayerMover.SetAnimation("parkour_end", loop: false);
		await UniTask.WaitUntil(() => Mathf.Abs(targetPosition.x - PlayerMover.VisualBottom.x) <= 0.01f || PlayerMover.VisualBottom.x >= targetPosition.x == PlayerMover.FacingDirection.GetSignX() > 0, PlayerLoopTiming.Update, token);
		if (!token.IsCancellationRequested)
		{
			psm.SetStateOptionActive(StateOption.GravityFree, active: false);
			PlayerMover.ResetRelativeVelocity();
			isMoving = false;
			await UniTask.WaitUntil(() => PlayerMover.IsOnGround, PlayerLoopTiming.Update, token);
			if (!token.IsCancellationRequested)
			{
				SoundManager.PlayOneShot("event:/SFX/Player/Move/SFX_Parkour_Landing");
				psm.ChangeState(psm.IdleState);
			}
		}
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.GravityFree, active: false);
	}

	public void SetTargetPosition(Vector2 edgePos)
	{
		targetPosition = edgePos;
	}
}
