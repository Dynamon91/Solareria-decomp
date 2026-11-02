using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using UnityEngine;

namespace SPR;

public class PlayerRollingState : PlayerDashState
{
	private int frameCount;

	private bool onPostdelay;

	protected virtual Vector2 StartPosition { get; set; }

	protected virtual Vector2 EndPosition { get; set; }

	public PlayerRollingState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	protected override void OnEnter(IState prevState)
	{
		base.OnEnter(prevState);
		PlayerState.PlayerStat.GetStat<Stamina>().AddStamina(-PlayerAsyncState.pcp.RollingStamina);
		PlayerMover.ResetRelativeVelocity();
		PlayerMover.Flip(PlayerState.PlayerController.horizontal);
		frameCount = 0;
		onPostdelay = false;
		PlayerDashState.SoundManager.PlayOneShot("event:/SFX/Player/Move/SFX_Rolling");
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		PlayerMover.SetAnimation("roll", loop: false);
		Direction direction = PlayerMover.FacingDirection;
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(PlayerAsyncState.pcp.RollingPredelayFrame, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		StartPosition = PlayerMover.VisualCenter;
		PlayerMover.ApplyRelativeVelocity(direction, PlayerAsyncState.pcp.RollingSpeed, faceJump: false);
		bool flag = !base.CanPassWall;
		if (!flag)
		{
			flag = !(await PassWallAsync(PlayerAsyncState.pcp.RollingSpeed, PlayerAsyncState.pcp.RollingActionFrame, token));
		}
		if (flag)
		{
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(PlayerAsyncState.pcp.RollingActionFrame, token);
		}
		if (!token.IsCancellationRequested)
		{
			PlayerMover.ResetRelativeVelocity();
			EndPosition = PlayerMover.VisualCenter;
			onPostdelay = true;
			psm.SetStateOptionActive(StateOption.Controllable, active: true);
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(PlayerAsyncState.pcp.RollingPostdelayFrame, token);
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

	protected override void OnFixedUpdate(float fdt)
	{
		base.OnFixedUpdate(fdt);
		frameCount++;
		float time = Time.time;
		if (PlayerMover.IsOnGround && psm.JumpUpInputItem.CanExecute(time))
		{
			psm.JumpUpInputItem.Execute();
			return;
		}
		if (psm.OverheatInputItem.CanExecute(Time.time))
		{
			psm.OverheatInputItem.Execute();
			return;
		}
		if (frameCount == PlayerAsyncState.pcp.rollingDodgeEndFrame)
		{
			psm.SetStateOptionActive(StateOption.Dodge, active: false);
		}
		if (onPostdelay && psm.GuardInputItem.CanExecute(time))
		{
			psm.GuardInputItem.Execute();
		}
	}
}
