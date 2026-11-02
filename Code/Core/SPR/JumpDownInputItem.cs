namespace SPR;

public class JumpDownInputItem : InputItem
{
	private bool holdJump;

	private readonly float earlyInputAcceptTime;

	public JumpDownInputItem(IState state)
		: base(state)
	{
		earlyInputAcceptTime = Util.FrameToSeconds(InputItem.pcp.EarlyJumpInputAcceptFrame);
	}

	public override void UpdateInput(float currentTime)
	{
		if (!InputItem.PlayerMover.IsOnOneWayPlatform)
		{
			return;
		}
		if (InputItem.PlayerController.jump)
		{
			if (!holdJump)
			{
				holdJump = true;
				if (InputItem.PlayerController.vertical < 0)
				{
					base.InputTime = currentTime;
				}
			}
		}
		else
		{
			holdJump = false;
		}
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return false;
		}
		if (!InputItem.PlayerMover.IsOnOneWayPlatform)
		{
			return false;
		}
		return GetInputDeltaTime(currentTime) <= earlyInputAcceptTime;
	}
}
