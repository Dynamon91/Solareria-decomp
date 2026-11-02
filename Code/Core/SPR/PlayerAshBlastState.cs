using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using UnityEngine;

namespace SPR;

public class PlayerAshBlastState : PlayerAsyncState
{
	private readonly float continuousAcceptTime;

	private const int TotalAttackCount = 3;

	private const string AnimName = "ash_blast";

	private int attackIndex;

	private float attackEndTime = -999f;

	public PlayerAshBlastState(PlayerStateMachine machine)
		: base(machine)
	{
		continuousAcceptTime = Util.FrameToSeconds(PlayerAsyncState.pcp.ComboSkillContinuousInputFrame);
	}

	protected override void OnEnter(IState prevState)
	{
		attackIndex = ((psm.AshBlastInputItem.InputTime - attackEndTime <= continuousAcceptTime) ? ((attackIndex + 1) % 3) : 0);
		if (attackIndex > 0)
		{
			PlayerMover.SetAnimation("idle");
		}
		else
		{
			PlayerState.PlayerStat.GetStat<Stamina>().AddStamina(-PlayerAsyncState.pcp.ComboSkillStamina);
		}
		string animationName = string.Format("{0}_{1}", "ash_blast", attackIndex + 1);
		PlayerMover.SetAnimation(animationName, loop: false);
		if (attackIndex == 0 && !PlayerMover.IsOnGround)
		{
			psm.FirstAirAttackInputItem.ResetTime();
			psm.SecondAirAttackInputItem.ResetTime();
		}
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.GravityFree, active: false);
		if (nextState != this)
		{
			psm.AshBlastInputItem.ResetInput();
		}
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		psm.SetStateOptionActive(StateOption.GravityFree, active: true);
		PlayerMover.ResetRelativeVelocity();
		AttackFrame frames = PlayerAsyncState.pcp.ComboSkillFrames[attackIndex];
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(frames.Predelay, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		PlayerMover.ApplyRelativeVelocity(PlayerMover.FacingDirection, PlayerAsyncState.pcp.ComboSkillSpeed, faceJump: false);
		PlayerMover.ClearDamagedList();
		int actionFrame = frames.Action;
		for (int i = 0; i < actionFrame; i++)
		{
			PlayerMover.HandleAttackHitBox("ash_blast", attackIndex, AttackAttribute.Default);
			await PlayerUniTaskUtil.WaitOneFrame();
			if (token.IsCancellationRequested)
			{
				return;
			}
		}
		PlayerMover.ResetRelativeVelocity();
		await PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(frames.Postdelay, token);
		if (!token.IsCancellationRequested)
		{
			psm.SetStateOptionActive(StateOption.GravityFree, active: false);
			attackEndTime = Time.time;
			if (attackIndex + 1 < 3 && psm.AshBlastInputItem.CanExecute(Time.time))
			{
				psm.AshBlastInputItem.Execute();
			}
			else
			{
				psm.ChangeState(psm.IdleState);
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
}
