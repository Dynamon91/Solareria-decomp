using FSM;

namespace SPR.FSM;

public abstract class PlayerUpgradeableState : PlayerAsyncState
{
	protected bool IsUpgraded { get; private set; }

	protected PlayerUpgradeableState(PlayerStateMachine machine)
		: base(machine)
	{
	}

	public void Upgrade(bool upgrade)
	{
		IsUpgraded = upgrade;
		OnUpgrade(upgrade);
	}

	protected virtual void OnUpgrade(bool upgrade)
	{
	}
}
