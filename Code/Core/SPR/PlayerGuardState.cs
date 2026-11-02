using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using LapinLibrary;
using Library;
using UnityEngine;

namespace SPR;

public class PlayerGuardState : PlayerState
{
	private enum GuardState
	{
		Hold,
		Auto,
		Postdelay
	}

	private GuardState guardState;

	private int autoGuardFrameCount;

	private bool isJumping;

	private bool canMove;

	private bool isMoving;

	private CancellationTokenSource knockBackTokenSource;

	public bool HadGuard => guardState == GuardState.Auto;

	public bool IsInPostDelay => guardState == GuardState.Postdelay;

	private static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	private static InputManager InputManager => Game.Instance.InputManager;

	public PlayerGuardState(PlayerStateMachine machine)
		: base(machine)
	{
	}

	public override void Enter(IState prevState)
	{
		guardState = GuardState.Hold;
		autoGuardFrameCount = 0;
		psm.SetStateOptionActive(StateOption.Controllable | StateOption.BlockStaminaRegain | StateOption.Guard, active: true);
		if (prevState == psm.ParryState)
		{
			PlayerMover.SetAnimation("dagger/guard_loop", loop: false);
		}
		else
		{
			PlayerMover.SetAnimation("dagger/guard", loop: false);
			PlayerMover.AddAnimation("dagger/guard_loop");
			Game.Instance.SoundManager.PlayOneShot("event:/SFX/Player/Combat/SFX_Player_Guard_Up");
		}
		isJumping = prevState == psm.JumpUpState || prevState == psm.FallState;
		if (!isJumping)
		{
			PlayerMover.HorizontalMove(0f);
		}
		canMove = true;
		isMoving = false;
		float horizontal = PlayerState.PlayerController.horizontal;
		if (horizontal != 0f && horizontal.GetSign() != (PlayerMover.IsFacingRight ? 1 : (-1)))
		{
			PlayerMover.Flip(horizontal);
		}
	}

	public override void Exit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.Controllable | StateOption.BlockStaminaRegain | StateOption.Guard, active: false);
		knockBackTokenSource?.Cancel();
		PlayerMover.HorizontalMove(0f);
	}

	public override void FixedUpdate(float fdt)
	{
		if (psm.OverheatInputItem.CanExecute(Time.time))
		{
			psm.OverheatInputItem.Execute();
			return;
		}
		if (guardState == GuardState.Auto)
		{
			if (autoGuardFrameCount < pcp.AutoGuardFrame)
			{
				autoGuardFrameCount++;
				return;
			}
			if (!psm.GuardInputItem.HoldGuard)
			{
				psm.ChangeState(psm.IdleState);
				return;
			}
			guardState = GuardState.Hold;
		}
		if (isJumping)
		{
			if (PlayerMover.IsOnGround)
			{
				isJumping = false;
			}
			else if (canMove)
			{
				MoveOnAir();
			}
		}
		if (!isJumping && canMove)
		{
			MoveOnGround(fdt);
		}
		if (guardState == GuardState.Hold && !psm.GuardInputItem.HoldGuard)
		{
			PlayerState.PlayerStat.GetStat<Stamina>().ResetStaminaRegainCycle();
			if (!Game.Instance.InputManager.IsCurrentControlTarget(PlayerState.PlayerController))
			{
				psm.ChangeState(psm.IdleState);
			}
			else
			{
				guardState = GuardState.Postdelay;
			}
		}
		else if (guardState == GuardState.Postdelay && !psm.TryChangeStateOnIdle())
		{
			if (psm.TryRun())
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
			else
			{
				psm.ChangeState(psm.IdleState);
			}
		}
	}

	private void MoveOnAir()
	{
		if (InputManager.IsCurrentControlTarget(PlayerState.PlayerController) && !psm.JumpUpInputItem.Hold)
		{
			PlayerMover.JumpCancel();
		}
		float horizontal = PlayerState.PlayerController.horizontal;
		PlayerMover.HorizontalMove(horizontal);
	}

	private void MoveOnGround(float fdt)
	{
		int sign = PlayerState.PlayerController.horizontal.GetSign();
		float dir = (float)(sign * PlayerControlParameter.Instance.GuardMoveSpeed) * fdt;
		PlayerMover.HorizontalMove(dir);
		if (!isMoving && sign != 0)
		{
			isMoving = true;
			PlayerMover.SetAnimation("dagger/guard_walk");
		}
		else if (isMoving && sign == 0)
		{
			isMoving = false;
			PlayerMover.SetAnimation("dagger/guard_loop");
		}
	}

	public void PlayGuard()
	{
		guardState = GuardState.Auto;
		autoGuardFrameCount = 0;
		PoolableTimeLimitObject poolableTimeLimitObject = Game.Instance.ObjectPoolManager.GetObject<PoolableTimeLimitObject>("VFX/Stone_Guard", PlayerMover.VisualCenter);
		poolableTimeLimitObject.Initialize("VFX/Stone_Guard", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
		poolableTimeLimitObject.GetComponent<ParticleSystem>().Play();
		Game.Instance.SoundManager.PlayOneShot("event:/SFX/Player/Combat/SFX_Player_Guard");
		GuardKnockbackAsync(TaskUtil.RefreshToken(ref knockBackTokenSource)).GetAwaiter().OnCompleted(delegate
		{
			knockBackTokenSource?.Cancel();
		});
	}

	private async UniTask GuardKnockbackAsync(CancellationToken token)
	{
		canMove = false;
		int guardKnockbackFrame = pcp.GuardKnockbackFrame;
		if (guardKnockbackFrame == 0)
		{
			Debug.LogError("PlayerControlParameter에서 GuardKnockbackFrame 값을 입력해주세요.");
			return;
		}
		await PlayerMover.KnockBackAsync(guardKnockbackFrame, PlayerMover.LastHitInfo.GuardKnockBack, token);
		PlayerMover.HorizontalMove(0f);
		canMove = true;
	}
}
