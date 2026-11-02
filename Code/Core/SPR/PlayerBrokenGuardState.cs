using System;
using System.Threading;
using FSM;
using LapinLibrary;
using UnityEngine;

namespace SPR;

public class PlayerBrokenGuardState : PlayerState
{
	private int durationFrame;

	private int frameCount;

	private CancellationTokenSource knockbackTokenSource;

	private const int GuardRollingAcceptFrame = 25;

	private const int GuardRollingExecuteFrame = 20;

	public event Action OnBroken = delegate
	{
	};

	public PlayerBrokenGuardState(PlayerStateMachine machine)
		: base(machine)
	{
	}

	public override void Enter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.BlockStaminaRegain | StateOption.BlockHeavyCharge, active: true);
		PlayerMover.HorizontalMove(0f);
		PlayerMover.SetAnimation("guard_break", loop: false);
		Game.Instance.SoundManager.PlayOneShot("event:/SFX/Player/Combat/SFX_Player_Guard_Break");
		durationFrame = Util.SecondsToFrame(PlayerMover.GetAnimDuration("guard_break"));
		frameCount = 0;
		CancellationToken token = TaskUtil.RefreshToken(ref knockbackTokenSource);
		PlayerMover.KnockBackAsync(PlayerControlParameter.Instance.GuardKnockbackFrame, PlayerMover.LastHitInfo.GuardKnockBack, token).GetAwaiter().OnCompleted(delegate
		{
			knockbackTokenSource?.Cancel();
		});
		this.OnBroken?.Invoke();
	}

	public override void Exit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.Controllable | StateOption.BlockStaminaRegain | StateOption.BlockHeavyCharge, active: false);
		PlayerState.PlayerStat.GetStat<Stamina>().ResetStaminaRegainCycle();
		knockbackTokenSource.Cancel();
	}

	public override void FixedUpdate(float fdt)
	{
		frameCount++;
		if (psm.OverheatInputItem.CanExecute(Time.time))
		{
			psm.OverheatInputItem.Execute();
		}
		else if (frameCount == durationFrame - 25)
		{
			psm.SetStateOptionActive(StateOption.Controllable, active: true);
		}
		else if (frameCount == durationFrame - 20)
		{
			float time = Time.time;
			if (psm.RollingInputItem.CanExecute(time))
			{
				psm.RollingInputItem.Execute();
			}
			else if (psm.GuardInputItem.CanExecute(time))
			{
				psm.GuardInputItem.Execute();
			}
		}
		else if (frameCount >= durationFrame)
		{
			psm.ChangeState(psm.IdleState);
		}
	}
}
