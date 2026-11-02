using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using UnityEngine;

namespace SPR;

public class PlayerGroundAttackState : PlayerAsyncState
{
	private GroundAttackTask attackTask;

	private readonly List<InputItem> inputItems = new List<InputItem>();

	private static readonly List<Type> NextInputItemTypes = new List<Type>
	{
		typeof(JumpUpInputItem),
		typeof(DoubleJumpUpInputItem),
		typeof(AshBreakInputItem),
		typeof(AshBlastInputItem),
		typeof(BurstInputItem),
		typeof(BasicAttackInputItem),
		typeof(ChargeAttackInputItem),
		typeof(FirstAirAttackInputItem),
		typeof(JumpDashInputItem),
		typeof(RollingInputItem),
		typeof(GuardInputItem),
		typeof(SpecialParryInputItem)
	};

	public bool IsLastAttack => attackTask.IsLastAttackPending;

	public int CurrentAttackIndex => attackTask.AttackIndex;

	public PlayerGroundAttackState(PlayerStateMachine psm)
		: base(psm)
	{
		attackTask = new GroundAttackTask();
	}

	public void ChangeAttackTask(GroundAttackTask groundAttackTask)
	{
		attackTask = groundAttackTask;
	}

	protected override void OnEnter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.Controllable, active: true);
		if (inputItems.Count != 0)
		{
			return;
		}
		foreach (Type nextInputItemType in NextInputItemTypes)
		{
			inputItems.Add(PlayerMover.PSM.GetInputItem(nextInputItemType));
		}
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(attackTask.StateOption | StateOption.Controllable, active: false);
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		await attackTask.Attack(token);
		int frameCount = 0;
		while (true)
		{
			if (token.IsCancellationRequested || psm.TryChangeSkillState(inputItems))
			{
				return;
			}
			if (!PlayerMover.IsOnGround)
			{
				psm.ChangeState(psm.FallState);
				return;
			}
			if (frameCount >= PlayerAsyncState.pcp.PostMovingBlockFrame)
			{
				break;
			}
			frameCount++;
			await PlayerUniTaskUtil.WaitOneFrame();
		}
		psm.ChangeState(psm.IdleState);
	}

	protected override void OnFixedUpdate(float dt)
	{
		attackTask.FixedUpdate();
		if (psm.GuardInputItem.CanExecute(Time.time))
		{
			psm.GuardInputItem.Execute();
		}
		else if (psm.RollingInputItem.CanExecute(Time.time))
		{
			psm.RollingInputItem.Execute();
		}
		else if (psm.OverheatInputItem.CanExecute(Time.time))
		{
			psm.OverheatInputItem.Execute();
		}
	}

	public void SetAttackEndTime(float time)
	{
		attackTask.SetAttackEndTime(time);
	}
}
