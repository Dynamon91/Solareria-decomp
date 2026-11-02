using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using UnityEngine;

namespace SPR;

public class PlayerJumpDashState : PlayerDashState
{
	private const string AnimName = "air_dash";

	protected virtual float DashSpeed => PlayerAsyncState.pcp.JumpDashSpeed;

	protected virtual int DashFrame => PlayerAsyncState.pcp.JumpDashAttackFrame.Action;

	protected virtual Vector2 StartPosition { get; set; }

	protected virtual Vector2 EndPosition { get; set; }

	public PlayerJumpDashState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	protected override void OnEnter(IState prevState)
	{
		base.OnEnter(prevState);
		PlayerState.PlayerStat.GetStat<Stamina>().AddStamina(-PlayerAsyncState.pcp.JumpDashStamina);
		psm.FirstAirAttackInputItem.ResetTime();
		psm.SecondAirAttackInputItem.ResetTime();
		PlayerMover.SetAnimation("air_dash", loop: false);
		PlayerDashState.SoundManager.PlayOneShot("event:/SFX/Player/Move/SFX_Rolling");
	}

	protected override void OnFixedUpdate(float fdt)
	{
		base.OnFixedUpdate(fdt);
		float time = Time.time;
		if (psm.DoubleJumpUpInputItem.CanExecute(time))
		{
			psm.DoubleJumpUpInputItem.Execute();
		}
		else
		{
			psm.StateOption.HasFlag(StateOption.Dodge);
		}
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		await MoveAsync(token);
		if (!token.IsCancellationRequested)
		{
			psm.ChangeState(psm.FallState);
		}
	}

	private async UniTask MoveAsync(CancellationToken token)
	{
		AttackFrame attackFrame = PlayerAsyncState.pcp.JumpDashAttackFrame;
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(attackFrame.Predelay, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		StartPosition = PlayerMover.VisualCenter;
		ApplyDashVelocity();
		bool flag = !base.CanPassWall;
		if (!flag)
		{
			flag = !(await PassWallAsync(PlayerAsyncState.pcp.JumpDashSpeed, PlayerAsyncState.pcp.JumpDashAttackFrame.Action, token));
		}
		if (flag)
		{
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(attackFrame.Action, token);
		}
		if (!token.IsCancellationRequested)
		{
			psm.SetStateOptionActive(StateOption.Dodge, active: false);
			PlayerMover.ResetRelativeVelocity();
			EndPosition = PlayerMover.VisualCenter;
			psm.SetStateOptionActive(StateOption.Controllable, active: true);
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(attackFrame.Postdelay, token);
			if (!token.IsCancellationRequested)
			{
				ChangeNextState();
			}
		}
	}

	protected override void OnExit(IState nextState)
	{
		base.OnExit(nextState);
		psm.SetStateOptionActive(StateOption.Controllable, active: false);
	}

	private void ApplyDashVelocity()
	{
		Direction direction = ((psm.MoveDirectionInt(includeFacing: true) > 0) ? Direction.Right : Direction.Left);
		PlayerMover.ApplyRelativeVelocity(direction, PlayerAsyncState.pcp.JumpDashSpeed, faceJump: true);
	}
}
