using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using UnityEngine;

namespace SPR;

public abstract class PlayerAirAttackState : PlayerAsyncState
{
	private readonly string effectName;

	private readonly AttackTask attackTask;

	private readonly List<Type> inputItemTypes;

	private List<InputItem> inputItems;

	protected PlayerAirAttackState(PlayerStateMachine machine, string effectName)
		: base(machine)
	{
		this.effectName = effectName;
		attackTask = GetAttackTask();
		inputItemTypes = GetInputItemTypes();
	}

	protected abstract AttackTask GetAttackTask();

	protected abstract List<Type> GetInputItemTypes();

	protected override void OnEnter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.Controllable, active: true);
		if (inputItems != null)
		{
			return;
		}
		inputItems = new List<InputItem>();
		foreach (Type inputItemType in inputItemTypes)
		{
			inputItems.Add(PlayerMover.PSM.GetInputItem(inputItemType));
		}
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(attackTask.StateOption | StateOption.Controllable, active: false);
		if (!string.IsNullOrEmpty(effectName))
		{
			PlayerMover.DisableVFX(effectName);
		}
	}

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		await attackTask.Attack(token);
	}

	protected override void OnFixedUpdate(float fdt)
	{
		attackTask.FixedUpdate();
		float time = Time.time;
		if (psm.GuardInputItem.CanExecute(time))
		{
			psm.GuardInputItem.Execute();
		}
		else if (psm.RollingInputItem.CanExecute(time))
		{
			psm.RollingInputItem.Execute();
		}
		else
		{
			if (attackTask.IsValid())
			{
				return;
			}
			float horizontal = PlayerState.PlayerController.horizontal;
			PlayerMover.HorizontalMove(horizontal);
			PlayerMover.Flip(horizontal);
			if (!psm.TryChangeSkillState(inputItems))
			{
				Vector2 edgePos;
				if (psm.TryLandingHard())
				{
					psm.ChangeState(psm.LandingHardState);
				}
				else if (psm.TryParkour(out edgePos))
				{
					((PlayerParkourState)psm.ParkourState).SetTargetPosition(edgePos);
					psm.ChangeState(psm.ParkourState);
				}
				else if (psm.TryIdle())
				{
					psm.ChangeState(psm.IdleState);
				}
				else if (psm.TryWalk())
				{
					psm.ChangeState(psm.WalkState);
				}
				else if (psm.TryRun())
				{
					psm.ChangeState(psm.RunState);
				}
			}
		}
	}
}
