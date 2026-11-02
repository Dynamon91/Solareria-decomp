using Library;

namespace SPR;

public class UpgradePyronActionPart : SimpleStatEffectPart
{
	private const string PyronUpgradeID = "pyron_upgrade_flame";

	private const string HeavyUpgradeID = "pyron_heavy_upgrade_flame";

	public override void OnEquip()
	{
		base.OnEquip();
		Part.PlayerMover.PSM.PyronAttackState.CastObject<PlayerParryActionState>().SetFlameID("pyron_upgrade_flame");
		Part.PlayerMover.PSM.HyperPyronAttackState.CastObject<PlayerParryActionState>().SetFlameID("pyron_heavy_upgrade_flame");
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		Part.PlayerMover.PSM.PyronAttackState.CastObject<PlayerParryActionState>().SetFlameID("pyron_flame");
		Part.PlayerMover.PSM.HyperPyronAttackState.CastObject<PlayerParryActionState>().SetFlameID("pyron_heavy_flame");
	}
}
