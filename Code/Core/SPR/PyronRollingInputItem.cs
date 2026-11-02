using System;

namespace SPR;

public class PyronRollingInputItem : RollingInputItem
{
	private Action onCancelPyron;

	public PyronRollingInputItem(IState state)
		: base(state)
	{
	}

	protected override bool CheckCondition(float currentTime)
	{
		return GetInputDeltaTime(currentTime) <= EarlyInputAcceptTime;
	}

	protected override void OnExecute()
	{
		base.OnExecute();
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			InputItem.PlayerMover.PSM.ParryAttackInputItem.Active = false;
			onCancelPyron?.Invoke();
		}
	}

	public void SetCancelPyronAction(Action action)
	{
		onCancelPyron = action;
	}
}
