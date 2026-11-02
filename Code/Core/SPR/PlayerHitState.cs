using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using Library;
using UnityEngine;

namespace SPR;

public class PlayerHitState : PlayerAsyncState
{
	public const int ActionFrameCount = 10;

	public const int PostdelayFrameCount = 20;

	private bool canMove;

	private bool isHitBefore;

	private int frameCount;

	private const string StartAnimName = "hit_start";

	private const string EndAnimName = "hit_end";

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public PlayerHitState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	protected override void OnEnter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.BlockHeavyCharge, active: true);
		PlayerMover.ResetRelativeVelocity();
		PlayerMover.Flip(-PlayerMover.LastHitDirection);
		canMove = false;
		SoundManager.PlayOneShot("event:/SFX/Player/Hit/SFX_Hit");
		isHitBefore = prevState == this;
		frameCount = 0;
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.Controllable | StateOption.BlockHeavyCharge, active: false);
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		if (isHitBefore)
		{
			PlayerMover.SetAnimation("idle", loop: false);
		}
		PlayerMover.SetAnimation("hit_start", loop: false, 1f, 0, 0f, vfxEnable: false);
		int direction = ((PlayerMover.LastHitDirection != 0) ? PlayerMover.LastHitDirection : PlayerMover.FacingDirection.GetSignX());
		HitKnockBackAsync(direction, PlayerMover.LastHitInfo.KnockBack, token).Forget();
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(10, token);
		if (!token.IsCancellationRequested)
		{
			PlayerMover.SetAnimation("hit_end", loop: false);
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(20, token);
			if (!token.IsCancellationRequested)
			{
				psm.ChangeState(psm.IdleState);
			}
		}
	}

	protected override void OnFixedUpdate(float fdt)
	{
		frameCount++;
		if (frameCount == PlayerAsyncState.pcp.moveAcceptFrameOnHit - PlayerAsyncState.pcp.guardEarlyAcceptFrame)
		{
			psm.SetStateOptionActive(StateOption.Controllable, active: true);
		}
		if (frameCount == PlayerAsyncState.pcp.moveAcceptFrameOnHit)
		{
			canMove = true;
		}
		if (canMove)
		{
			float time = Time.time;
			if (psm.OverheatInputItem.CanExecute(Time.time))
			{
				psm.OverheatInputItem.Execute();
			}
			else if (psm.RollingInputItem.CanExecute(time))
			{
				psm.RollingInputItem.Execute();
			}
			else if (psm.GuardInputItem.CanExecute(time))
			{
				psm.GuardInputItem.Execute();
			}
			else if (psm.TryRun())
			{
				psm.ChangeState(psm.RunState);
			}
			else if (psm.TryWalk())
			{
				psm.ChangeState(psm.WalkState);
			}
			else if (psm.TryFall())
			{
				psm.ChangeState(psm.FallState);
			}
		}
	}
}
