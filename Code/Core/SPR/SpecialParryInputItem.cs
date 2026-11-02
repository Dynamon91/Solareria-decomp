namespace SPR;

public class SpecialParryInputItem : InputItem
{
	private bool holdGuard;

	public SpecialParryInputItem(IState state)
		: base(state)
	{
	}

	public override void UpdateInput(float currentTime)
	{
		if (!Game.Instance.InputManager.IsCurrentControlTarget(InputItem.PlayerController))
		{
			base.InputTime = -999f;
		}
		else if (InputItem.PlayerController.specialParry)
		{
			base.InputTime = currentTime;
		}
	}

	protected override bool CheckCondition(float currentTime)
	{
		return GetInputDeltaTime(currentTime) <= Util.FrameToSeconds(InputItem.pcp.EarlyAttackInputAcceptFrame);
	}
}
