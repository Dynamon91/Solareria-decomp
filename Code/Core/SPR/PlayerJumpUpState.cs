using System;
using System.Collections.Generic;
using FSM;
using Library;
using UnityEngine;

namespace SPR;

public class PlayerJumpUpState : PlayerState
{
	private List<InputItem> inputItems;

	private static readonly List<Type> NextInputItemTypes = new List<Type>
	{
		typeof(AshBlastInputItem),
		typeof(AshBreakInputItem),
		typeof(ChargeAttackInputItem),
		typeof(FirstAirAttackInputItem),
		typeof(JumpDashInputItem),
		typeof(FastDownInputItem),
		typeof(GuardInputItem),
		typeof(SpecialParryInputItem),
		typeof(OverheatInputItem)
	};

	private int frameCount;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	public PlayerJumpUpState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	public override void Enter(IState prevState)
	{
		SetStartAnimation();
		frameCount = 0;
		Vector2 jumpVelocity = pcp.JumpVelocity;
		float x = jumpVelocity.x;
		float y = jumpVelocity.y;
		float horizontal = PlayerState.PlayerController.horizontal;
		x = ((horizontal == 0f) ? 0f : (x * (float)horizontal.GetSign()));
		PlayerMover.ApplyRelativeVelocity(x, y, faceJump: true);
		psm.SetStateOptionActive(StateOption.Controllable, active: true);
		psm.FirstAirAttackInputItem.ResetTime();
		psm.SecondAirAttackInputItem.ResetTime();
		if (prevState.GetType() != GetType())
		{
			PoolableTimeLimitObject poolableTimeLimitObject = Game.Instance.ObjectPoolManager.GetObject<PoolableTimeLimitObject>("Jump.prefab", PlayerMover.VisualBottom);
			poolableTimeLimitObject.Initialize("Jump.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
			SoundManager.PlayOneShot("event:/SFX/Player/Move/SFX_Jump");
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

	protected virtual void SetStartAnimation()
	{
		PlayerMover.SetAnimation("jump_up", loop: false);
		PlayerMover.AddAnimation("jump_up_loop");
	}

	public override void Exit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.Controllable, active: false);
	}

	public override void FixedUpdate(float dt)
	{
		frameCount++;
		float horizontal = PlayerState.PlayerController.horizontal;
		PlayerMover.HorizontalMove(horizontal);
		PlayerMover.Flip(horizontal);
		_ = Time.time;
		if (frameCount > pcp.jumpMinFrame && InputManager.IsCurrentControlTarget(PlayerState.PlayerController) && !psm.JumpUpInputItem.Hold)
		{
			PlayerMover.JumpCancel();
		}
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
