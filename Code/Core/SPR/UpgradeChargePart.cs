namespace SPR;

public class UpgradeChargePart : SimpleStatEffectPart
{
	private const string UpgradeBurnID = "charge_upgrade_burn";

	private bool isApplied;

	public override void OnEquip()
	{
		base.OnEquip();
		if (isApplied)
		{
			Part.PlayerMover.PSM.ChargeAttackState.onAttack -= AddBurnEffect;
		}
		Part.PlayerMover.PSM.ChargeAttackState.onAttack += AddBurnEffect;
		isApplied = true;
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		if (isApplied)
		{
			Part.PlayerMover.PSM.ChargeAttackState.onAttack -= AddBurnEffect;
			isApplied = false;
		}
	}

	private void AddBurnEffect(IDamageable damageable)
	{
		if (damageable is Enemy enemy)
		{
			Part.Player.PlayerStat.MakeEnemyBurn(enemy, "charge_upgrade_burn");
		}
	}
}
