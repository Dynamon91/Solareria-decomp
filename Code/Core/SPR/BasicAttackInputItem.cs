namespace SPR;

public class BasicAttackInputItem : InputItem
{
	private readonly float earlyAttackInputTime;

	public BasicAttackInputItem(IState state)
		: base(state)
	{
		earlyAttackInputTime = Util.FrameToSeconds(InputItem.pcp.EarlyAttackInputAcceptFrame);
	}

	public override void UpdateInput(float currentTime)
	{
		if (!InputItem.PlayerMover.PSM.StateOption.HasFlag(StateOption.Guard) && InputItem.PlayerMover.IsOnGround && InputItem.PlayerController.attackDown)
		{
			base.InputTime = currentTime;
		}
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (!InputItem.PlayerMover.IsOnGround)
		{
			return false;
		}
		if (InputItem.PlayerMover.PSM.OverheatInputItem.CanExecute(currentTime))
		{
			return false;
		}
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return false;
		}
		if (GetInputDeltaTime(currentTime) > earlyAttackInputTime)
		{
			return false;
		}
		return true;
	}
}
