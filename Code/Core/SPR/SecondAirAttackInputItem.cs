namespace SPR;

public class SecondAirAttackInputItem : AirAttackInputItem
{
	public SecondAirAttackInputItem(IState state)
		: base(state)
	{
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (InputItem.PlayerMover.PSM.FirstAirAttackInputItem.CanExecute(currentTime))
		{
			return false;
		}
		return base.CheckCondition(currentTime);
	}
}
