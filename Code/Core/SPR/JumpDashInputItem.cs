using UnityEngine;

namespace SPR;

public class JumpDashInputItem : StaminaInputItem, IGroundInput
{
	private bool dashDown;

	private float lastAcceptTime;

	private readonly float earlyInputAcceptTime;

	public JumpDashInputItem(IState state)
		: base(state)
	{
		earlyInputAcceptTime = Util.FrameToSeconds(InputItem.pcp.EarlyDashInputAcceptFrame);
	}

	public override void UpdateInput(float currentTime)
	{
		if (InputItem.PlayerMover.IsOnGround)
		{
			lastAcceptTime = InputItem.PlayerMover.LastGroundedTime;
		}
		dashDown = InputItem.PlayerController.dashDown;
		if (dashDown)
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
		if (InputItem.PlayerMover.IsOnGround)
		{
			return false;
		}
		if (base.LastSuccessTime > lastAcceptTime)
		{
			return false;
		}
		IState currentState = InputItem.PlayerMover.PSM.CurrentState;
		if (currentState == InputItem.PlayerMover.PSM.JumpUpState || currentState == InputItem.PlayerMover.PSM.FallState)
		{
			if (dashDown)
			{
				return CheckStaminaCondition();
			}
			return false;
		}
		if (GetInputDeltaTime(currentTime) > earlyInputAcceptTime)
		{
			return false;
		}
		return CheckStaminaCondition();
	}

	public void ResetTime()
	{
		lastAcceptTime = Time.time;
	}
}
