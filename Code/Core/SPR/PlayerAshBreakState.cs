using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using UnityEngine;

namespace SPR;

public class PlayerAshBreakState : PlayerAsyncState
{
	private const string SkillDataID = "ash_break";

	public PlayerAshBreakState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	protected override void OnEnter(IState prevState)
	{
		PlayerState.PlayerStat.GetStat<Stamina>().AddStamina(-PlayerAsyncState.pcp.AshBreakStamina);
		PlayerMover.ResetRelativeVelocity();
		psm.SetStateOptionActive(StateOption.GravityFree, active: true);
		psm.FirstAirAttackInputItem.ResetTime();
		psm.SecondAirAttackInputItem.ResetTime();
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		PlayerMover.SetAnimation("ash_break_start", loop: false);
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(Util.SecondsToFrame(PlayerMover.GetAnimDuration("ash_break_start")), token);
		if (!token.IsCancellationRequested)
		{
			PlayerMover.SetAnimation("ash_break_end", loop: false);
			psm.SetStateOptionActive(StateOption.GravityFree, active: false);
			PlayerMover.ApplyRelativeVelocity(PlayerAsyncState.pcp.AshBreakVelocity * new Vector2((!PlayerMover.IsFacingRight) ? 1 : (-1), 1f), faceJump: false);
			PlayerMover.ClearDamagedList();
			PlayerMover.HandleAttackHitBox("ash_break", 0, AttackAttribute.Default);
			await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(PlayerAsyncState.pcp.ashBreakPostdelayFrame, token);
			if (!token.IsCancellationRequested)
			{
				psm.ChangeState(PlayerMover.IsOnGround ? psm.IdleState : psm.FallState);
			}
		}
	}

	protected override void OnFixedUpdate(float fdt)
	{
		if (psm.OverheatInputItem.CanExecute(Time.time))
		{
			psm.OverheatInputItem.Execute();
		}
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.GravityFree, active: false);
		psm.AshBreakInputItem.OnFinishedAshBreak();
	}
}
