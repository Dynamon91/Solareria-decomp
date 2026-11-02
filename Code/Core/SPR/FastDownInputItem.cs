namespace SPR;

public class FastDownInputItem : StaminaInputItem
{
	private readonly float earlyInputAcceptTime;

	public FastDownInputItem(IState state)
		: base(state)
	{
		earlyInputAcceptTime = Util.FrameToSeconds(InputItem.pcp.EarlyAttackInputAcceptFrame);
	}

	public override void UpdateInput(float currentTime)
	{
		if (!InputItem.PlayerMover.IsOnGround && InputItem.PlayerController.attackDown && InputItem.PlayerController.vertical < 0)
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
		if (GetInputDeltaTime(currentTime) > earlyInputAcceptTime)
		{
			return false;
		}
		return CheckStaminaCondition();
	}
}
