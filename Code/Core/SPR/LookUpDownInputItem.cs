using UnityEngine;

namespace SPR;

public class LookUpDownInputItem : InputItem
{
	private float holdingStartTime;

	public LookUpDownInputItem(IState state)
		: base(state)
	{
		holdingStartTime = -999f;
	}

	public override void UpdateInput(float currentTime)
	{
		if ((float)Mathf.Abs(InputItem.PlayerController.vertical) > InputItem.pcp.MoveSightInputThreshold && holdingStartTime < 0f)
		{
			holdingStartTime = currentTime;
		}
		else if ((float)Mathf.Abs(InputItem.PlayerController.vertical) <= InputItem.pcp.MoveSightInputThreshold || InputItem.PlayerController.horizontal != 0f || InputItem.PlayerController.jump || InputItem.PlayerController.attackDown || InputItem.PlayerController.dashDown || InputItem.PlayerController.healDown || InputItem.PlayerController.isDead || InputItem.PlayerController.mover.RelativeVelocity.magnitude > 0f)
		{
			holdingStartTime = -999f;
		}
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return false;
		}
		if (holdingStartTime > 0f)
		{
			return Mathf.Abs(GetDeltaTime(holdingStartTime, currentTime)) >= InputItem.pcp.LookUpDownMinimumInputDuration;
		}
		return false;
	}

	protected override void OnActiveChanged()
	{
		if (!base.Active)
		{
			holdingStartTime = -999f;
		}
	}
}
