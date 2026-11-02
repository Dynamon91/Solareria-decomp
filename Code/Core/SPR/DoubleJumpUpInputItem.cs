using UnityEngine;

namespace SPR;

public class DoubleJumpUpInputItem : InputItem, IGroundInput
{
	private float lastAcceptTime;

	private readonly float earlyInputAcceptTime;

	public bool Hold { get; private set; }

	public DoubleJumpUpInputItem(IState state)
		: base(state)
	{
		earlyInputAcceptTime = Util.FrameToSeconds(InputItem.pcp.EarlyJumpInputAcceptFrame);
	}

	public override void UpdateInput(float currentTime)
	{
		if (InputItem.PlayerMover.IsOnGround)
		{
			lastAcceptTime = InputItem.PlayerMover.LastGroundedTime;
		}
		if (InputItem.PlayerController.jump)
		{
			if (!Hold)
			{
				Hold = true;
				if (!InputItem.PlayerMover.IsOnGround)
				{
					base.InputTime = currentTime;
				}
			}
		}
		else
		{
			Hold = false;
		}
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return false;
		}
		if (InputItem.PlayerMover.PSM.JumpUpInputItem.CanExecute(currentTime))
		{
			return false;
		}
		if (base.LastSuccessTime > lastAcceptTime)
		{
			return false;
		}
		if (GetSuccessDeltaTime(currentTime) <= earlyInputAcceptTime)
		{
			return false;
		}
		return GetInputDeltaTime(currentTime) <= earlyInputAcceptTime;
	}

	public void ResetTime()
	{
		lastAcceptTime = Time.time;
	}
}
