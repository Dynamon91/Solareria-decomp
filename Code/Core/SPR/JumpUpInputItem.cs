namespace SPR;

public class JumpUpInputItem : InputItem
{
	private readonly float earlyInputAcceptTime;

	private readonly float lateJumpAcceptTime;

	public bool Hold { get; private set; }

	public JumpUpInputItem(IState state)
		: base(state)
	{
		earlyInputAcceptTime = Util.FrameToSeconds(InputItem.pcp.EarlyJumpInputAcceptFrame);
		lateJumpAcceptTime = Util.FrameToSeconds(InputItem.pcp.LateJumpAcceptFrame);
	}

	public override void UpdateInput(float currentTime)
	{
		if (InputItem.PlayerController.jump)
		{
			if (!Hold)
			{
				Hold = true;
				if (InputItem.PlayerController.vertical >= 0 || !InputItem.PlayerMover.IsOnOneWayPlatform)
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
		if (InputItem.PlayerMover.IsOnOneWayPlatform && InputItem.PlayerController.vertical < 0)
		{
			return false;
		}
		if (!InputItem.PlayerMover.IsOnGround)
		{
			if (GetDeltaTime(base.InputTime, InputItem.PlayerMover.LastGroundedTime) > lateJumpAcceptTime)
			{
				return false;
			}
			if (InputItem.PlayerMover.PSM.CurrentState == InputItem.PlayerMover.PSM.RollingState)
			{
				return false;
			}
		}
		if (base.LastSuccessTime >= InputItem.PlayerMover.LastGroundedTime)
		{
			return false;
		}
		return GetInputDeltaTime(currentTime) <= earlyInputAcceptTime;
	}
}
