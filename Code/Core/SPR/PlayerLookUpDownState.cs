using FSM;
using UnityEngine;

namespace SPR;

public class PlayerLookUpDownState : PlayerState
{
	private float enterDuration;

	private static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	public PlayerLookUpDownState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	public override void Enter(IState prevState)
	{
		if (PlayerState.PlayerController.vertical > 0)
		{
			PlayerMover.SetAnimation("cam_up");
		}
		else
		{
			PlayerMover.SetAnimation("cam_down", loop: false);
			PlayerMover.AddAnimation("cam_down_loop");
		}
		enterDuration = 0f;
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		enterDuration += fdt;
		if (!(enterDuration >= pcp.MoveSightHoldingDuration) && ((float)Mathf.Abs(PlayerState.PlayerController.vertical) < pcp.MoveSightInputThreshold || PlayerState.PlayerController.horizontal != 0f || PlayerState.PlayerController.jump || PlayerState.PlayerController.attackDown || PlayerState.PlayerController.dashDown || PlayerState.PlayerController.healDown || PlayerState.PlayerController.isDead || PlayerState.PlayerController.mover.RelativeVelocity.magnitude > 0f))
		{
			psm.ChangeState(psm.IdleState);
		}
	}
}
