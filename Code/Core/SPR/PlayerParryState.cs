using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using LapinLibrary;
using UnityEngine;

namespace SPR;

public class PlayerParryState : PlayerState
{
	private int currentFrame;

	private Vector2 knockbackMaxPos;

	private int hitDirection;

	private float jerk;

	private CancellationTokenSource knockbackTokenSource;

	private static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	public PlayerParryState(PlayerStateMachine machine)
		: base(machine)
	{
	}

	public override void Enter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.Controllable | StateOption.BlockStaminaRegain | StateOption.Parry, active: true);
		psm.ResetParryLock(onlyParryLock: true);
		currentFrame = 0;
		if (prevState != psm.ParryState)
		{
			PlayerMover.SetAnimation("dagger/parry", loop: false, 1f, 0, 0f, vfxEnable: false);
			PlayerMover.AddAnimation("dagger/guard_loop");
		}
		if (PlayerMover.LastHitInfo.ParryKnockBack != Vector2.zero)
		{
			int parryKnockbackFrame = pcp.parryKnockbackFrame;
			if (parryKnockbackFrame == 0)
			{
				Debug.LogError("PlayerControlParameter에서 ParryKnockbackFrame 값을 입력해주세요.");
				return;
			}
			CancellationToken token = TaskUtil.RefreshToken(ref knockbackTokenSource);
			PlayerMover.KnockBackAsync(parryKnockbackFrame, PlayerMover.LastHitInfo.ParryKnockBack, token).Forget();
		}
	}

	public override void Exit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.Controllable | StateOption.BlockStaminaRegain | StateOption.Parry, active: false);
		PlayerState.PlayerStat.GetStat<Stamina>().ResetStaminaRegainCycle();
		knockbackTokenSource?.Cancel();
	}

	public override void FixedUpdate(float fdt)
	{
		bool holdGuard = psm.GuardInputItem.HoldGuard;
		if (holdGuard || !psm.TryChangeStateOnIdle())
		{
			if (currentFrame < PlayerControlParameter.Instance.AutoParryFrame)
			{
				currentFrame++;
			}
			else
			{
				psm.ChangeState(holdGuard ? psm.GuardState : psm.IdleState);
			}
		}
	}
}
