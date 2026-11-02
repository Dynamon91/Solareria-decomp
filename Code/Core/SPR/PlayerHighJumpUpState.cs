using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using UnityEngine;

namespace SPR;

public class PlayerHighJumpUpState : PlayerAsyncState
{
	private List<InputItem> inputItems;

	private static readonly List<Type> NextInputItemTypes = new List<Type>
	{
		typeof(AshBlastInputItem),
		typeof(DoubleJumpUpInputItem),
		typeof(AshBreakInputItem),
		typeof(ChargeAttackInputItem),
		typeof(FirstAirAttackInputItem),
		typeof(JumpDashInputItem),
		typeof(GuardInputItem),
		typeof(SpecialParryInputItem),
		typeof(OverheatInputItem)
	};

	private readonly int jumpSpeed;

	public PlayerHighJumpUpState(PlayerStateMachine psm, int jumpSpeed)
		: base(psm)
	{
		this.jumpSpeed = jumpSpeed;
	}

	protected override void OnEnter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.Controllable, active: true);
		PlayerMover.ApplyRelativeVelocity(Direction.Up, jumpSpeed, faceJump: false);
		psm.DoubleJumpUpInputItem.ResetTime();
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

	protected override async UniTask ProgressAsync(CancellationToken token)
	{
		await PlayAnimationAsync("jump_up", loop: false, token);
		if (!token.IsCancellationRequested)
		{
			PlayerMover.SetAnimation("jump_up_loop");
		}
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.Controllable, active: false);
	}

	protected override void OnFixedUpdate(float fdt)
	{
		float horizontal = PlayerState.PlayerController.horizontal;
		PlayerMover.HorizontalMove(horizontal);
		PlayerMover.Flip(horizontal);
		_ = Time.time;
		if (!psm.TryChangeSkillState(inputItems))
		{
			if (psm.TryFall())
			{
				psm.ChangeState(psm.FallState);
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
