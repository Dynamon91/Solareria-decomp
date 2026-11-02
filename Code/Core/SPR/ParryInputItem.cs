namespace SPR;

public class ParryInputItem : GuardInputItem
{
	private int AcceptFrame => InputItem.pcp.ParryAcceptFrame;

	public ParryInputItem(IState state)
		: base(state)
	{
	}

	public override void UpdateInput(float currentTime)
	{
		if (InputItem.PlayerMover.PSM.GuardInputItem.Active)
		{
			base.InputTime = InputItem.PlayerMover.PSM.GuardInputItem.InputTime;
		}
		else
		{
			base.UpdateInput(currentTime);
		}
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (!InputItem.PlayerMover.PSM.CanControl)
		{
			return false;
		}
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return false;
		}
		if (InputItem.PlayerMover.PSM.SpecialParryInputItem.CanExecute(currentTime))
		{
			return false;
		}
		if (InputItem.PlayerMover.PSM.GuardInputItem.Active && InputItem.PlayerMover.PSM.GuardInputItem.LastSuccessTime < base.InputTime)
		{
			return false;
		}
		if (InputItem.PlayerMover.PSM.ParryAcceptFrame >= 0)
		{
			return InputItem.PlayerMover.PSM.ParryAcceptFrame <= AcceptFrame;
		}
		return false;
	}
}
