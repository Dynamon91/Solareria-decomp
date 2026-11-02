namespace SPR;

public class GuardInputItem : InputItem
{
	public bool HoldGuard { get; private set; }

	public GuardInputItem(IState state)
		: base(state)
	{
	}

	public override void UpdateInput(float currentTime)
	{
		if (CheckCancel())
		{
			HoldGuard = false;
			return;
		}
		if (!HoldGuard && InputItem.PlayerController.guardDown)
		{
			if (InputItem.PlayerMover.PSM.CanInputParry)
			{
				base.InputTime = currentTime;
			}
			HoldGuard = true;
			InputItem.PlayerMover.PSM.StartParryLock();
		}
		if (HoldGuard && InputItem.PlayerController.guardUp)
		{
			HoldGuard = false;
		}
	}

	private bool CheckCancel()
	{
		if (!InputItem.PlayerMover.PSM.CanControl)
		{
			return true;
		}
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return true;
		}
		if (InputItem.PlayerController.specialParry)
		{
			return true;
		}
		return !Game.Instance.InputManager.IsCurrentControlTarget(InputItem.PlayerController);
	}

	protected override bool CheckCondition(float currentTime)
	{
		if (InputItem.PlayerMover.PSM.ParryAttackInputItem.Active)
		{
			return false;
		}
		if (InputItem.PlayerMover.PSM.SpecialParryInputItem.CanExecute(currentTime))
		{
			return false;
		}
		if (InputItem.PlayerMover.PSM.ChargeAttackInputItem.IsHolding)
		{
			return false;
		}
		return HoldGuard;
	}

	protected override void OnActiveChanged()
	{
		if (!base.Active)
		{
			HoldGuard = false;
		}
	}
}
