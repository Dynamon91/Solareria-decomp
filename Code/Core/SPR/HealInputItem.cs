using UnityEngine;

namespace SPR;

public class HealInputItem : InputItem
{
	public HealInputItem(IState state)
		: base(state)
	{
	}

	public override void UpdateInput(float currentTime)
	{
		if (InputItem.PlayerController.healDown)
		{
			base.InputTime = currentTime;
		}
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return false;
		}
		if (InputItem.PlayerStat.GetStat<WarmhornCount>().Current <= 0f)
		{
			return false;
		}
		return Mathf.Abs(GetInputDeltaTime(currentTime)) <= 0.05f;
	}
}
