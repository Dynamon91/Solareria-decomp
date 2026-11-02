using FSM;
using UnityEngine;

namespace SPR;

public class PlayerAttackStuckState : PlayerState
{
	private int horizontalDirection;

	private float accel;

	private int currentFrame;

	public PlayerAttackStuckState(PlayerStateMachine machine)
		: base(machine)
	{
	}

	public override void Enter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.BlockHeavyCharge, active: true);
		PlayerMover.HorizontalMove(0f);
		PlayerMover.SetAnimation("stuck", loop: false);
		currentFrame = 0;
		Vector2 stuckKnockBackVelocity = PlayerControlParameter.Instance.StuckKnockBackVelocity;
		int stuckFrame = PlayerControlParameter.Instance.StuckFrame;
		if (stuckFrame == 0)
		{
			Debug.LogError("PlayerControlParameter에서 StuckFrame 값을 입력해주세요.");
			return;
		}
		accel = stuckKnockBackVelocity.magnitude / (float)stuckFrame;
		horizontalDirection = ((!PlayerMover.IsFacingRight) ? 1 : (-1));
		if (horizontalDirection != 0)
		{
			PlayerMover.ApplyRelativeVelocity((float)horizontalDirection * stuckKnockBackVelocity.x, stuckKnockBackVelocity.y, faceJump: false);
		}
		else
		{
			PlayerMover.ResetRelativeVelocity();
		}
	}

	public override void Exit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.BlockHeavyCharge, active: false);
	}

	public override void FixedUpdate(float fdt)
	{
		int num = PlayerControlParameter.Instance.StuckFrame + PlayerControlParameter.Instance.StuckPostdelayFrame;
		if (currentFrame >= num)
		{
			psm.ChangeState(psm.IdleState);
			return;
		}
		if (currentFrame < PlayerControlParameter.Instance.StuckFrame)
		{
			PlayerMover.DecelerateHorizontal(horizontalDirection, accel);
		}
		else if (currentFrame == PlayerControlParameter.Instance.StuckFrame)
		{
			PlayerMover.ResetRelativeVelocity();
		}
		currentFrame++;
	}
}
