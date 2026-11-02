using UnityEngine;

namespace SPR;

public class AirAttackInputItem : StaminaInputItem, IGroundInput
{
	private bool attackInput;

	private float attackAcceptTime;

	private readonly float earlyAttackInputTime;

	public AirAttackInputItem(IState state)
		: base(state)
	{
		earlyAttackInputTime = Util.FrameToSeconds(InputItem.pcp.EarlyAttackInputAcceptFrame);
		attackAcceptTime = -1f;
	}

	public override void UpdateInput(float currentTime)
	{
		if (InputItem.PlayerMover.IsOnGround)
		{
			attackAcceptTime = InputItem.PlayerMover.LastGroundedTime;
			return;
		}
		attackInput = InputItem.PlayerController.attackDown;
		if (attackInput)
		{
			base.InputTime = currentTime;
			SetInputThisFrame();
		}
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return false;
		}
		if (InputItem.PlayerMover.PSM.FastDownInputItem.CanExecute(currentTime))
		{
			return false;
		}
		if (InputItem.PlayerMover.IsOnGround)
		{
			return false;
		}
		if (GetInputDeltaTime(currentTime) > earlyAttackInputTime)
		{
			return false;
		}
		if (base.LastSuccessTime > attackAcceptTime)
		{
			return false;
		}
		IState currentState = InputItem.PlayerMover.PSM.CurrentState;
		if ((currentState == InputItem.PlayerMover.PSM.JumpUpState || currentState == InputItem.PlayerMover.PSM.FallState) && !attackInput)
		{
			return false;
		}
		return CheckStaminaCondition();
	}

	public void ResetTime()
	{
		attackAcceptTime = Time.time;
	}
}
