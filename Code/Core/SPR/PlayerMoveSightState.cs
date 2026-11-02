using FSM;
using UnityEngine;

namespace SPR;

public class PlayerMoveSightState : PlayerState
{
	private const float AnimationTransitionThreshold = 0.4f;

	private const string IdleAnim = "idle";

	private const string CamUpAnim = "cam_up";

	private const string CamDownAnim = "cam_down";

	private const string CamDownLoopAnim = "cam_down_loop";

	private static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	public PlayerMoveSightState(PlayerStateMachine psm)
		: base(psm)
	{
	}

	public override void Enter(IState prevState)
	{
		UpdateAnimationState(PlayerState.PlayerController.GetSightOffset().y);
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		Vector2 sightOffset = PlayerState.PlayerController.GetSightOffset();
		if (sightOffset.magnitude < pcp.MoveSightInputThreshold || PlayerState.PlayerController.horizontal != 0f || PlayerState.PlayerController.jump || PlayerState.PlayerController.attackDown || PlayerState.PlayerController.dashDown || PlayerState.PlayerController.healDown || PlayerState.PlayerController.isDead || PlayerState.PlayerController.mover.RelativeVelocity.magnitude > 0f)
		{
			psm.ChangeState(psm.IdleState);
		}
		else
		{
			UpdateAnimationState(sightOffset.y);
		}
	}

	private void UpdateAnimationState(float sightOffsetY)
	{
		string name = PlayerMover.GetCurrentAnim(0).Name;
		if (sightOffsetY > 0.4f)
		{
			if (name != "cam_up")
			{
				PlayerMover.SetAnimation("cam_up");
			}
		}
		else if (sightOffsetY < -0.4f)
		{
			if (name != "cam_down" && name != "cam_down_loop")
			{
				PlayerMover.SetAnimation("cam_down");
			}
			else if (name == "cam_down")
			{
				PlayerMover.AddAnimation("cam_down_loop");
			}
		}
		else if (name != "idle")
		{
			PlayerMover.SetAnimation("idle");
		}
	}
}
