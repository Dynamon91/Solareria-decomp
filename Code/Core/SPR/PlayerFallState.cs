using System;
using System.Collections.Generic;
using FSM;
using UnityEngine;

namespace SPR;

public class PlayerFallState : PlayerState
{
	private List<InputItem> inputItems;

	private static readonly List<Type> NextInputItemTypes = new List<Type>
	{
		typeof(AshBlastInputItem),
		typeof(AshBreakInputItem),
		typeof(ChargeAttackInputItem),
		typeof(FirstAirAttackInputItem),
		typeof(JumpDashInputItem),
		typeof(RollingInputItem),
		typeof(FastDownInputItem),
		typeof(GuardInputItem),
		typeof(SpecialParryInputItem),
		typeof(OverheatInputItem)
	};

	private bool canInputJump;

	public PlayerFallState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	public override void Enter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.Controllable, active: true);
		canInputJump = true;
		if (prevState == psm.DoubleJumpUpState)
		{
			PlayerMover.AddAnimation("jump_down", loop: false);
			PlayerMover.AddAnimation("jump_down_loop");
		}
		else if (prevState == psm.RollingState)
		{
			canInputJump = false;
			PlayerMover.SetAnimation("jump_down", loop: false);
			PlayerMover.AddAnimation("jump_down_loop");
		}
		else
		{
			PlayerMover.SetAnimation("jump_down_loop");
		}
		if (inputItems != null)
		{
			return;
		}
		inputItems = new List<InputItem>();
		foreach (Type nextInputItemType in NextInputItemTypes)
		{
			inputItems.Add(PlayerMover.PSM.GetInputItem(nextInputItemType));
		}
	}

	public override void Exit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.Controllable, active: false);
	}

	public override void FixedUpdate(float dt)
	{
		float horizontal = PlayerState.PlayerController.horizontal;
		PlayerMover.HorizontalMove(horizontal);
		PlayerMover.Flip(horizontal);
		float time = Time.time;
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
		else if (!TryNextJump(time))
		{
			psm.TryChangeSkillState(inputItems);
		}
	}

	private bool TryNextJump(float currentTime)
	{
		if (canInputJump && psm.JumpUpInputItem.CanExecute(currentTime))
		{
			psm.JumpUpInputItem.Execute();
			return true;
		}
		if (psm.DoubleJumpUpInputItem.CanExecute(currentTime))
		{
			psm.DoubleJumpUpInputItem.Execute();
			return true;
		}
		return false;
	}
}
