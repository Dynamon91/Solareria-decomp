using System;

namespace SPR;

public class ParryAttackInputItem : ParryActionInputItem
{
	protected override Func<bool> getInput => () => InputItem.PlayerController.action;

	public ParryAttackInputItem(IState state)
		: base(state)
	{
	}
}
