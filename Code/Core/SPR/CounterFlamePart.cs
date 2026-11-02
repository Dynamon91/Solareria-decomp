namespace SPR;

public class CounterFlamePart : SimpleStatEffectPart
{
	private const string ChargeCounterID = "charge_attack_counter_upgrade";

	private const string ParryCounterID = "heat_parry_counter_upgrade";

	public override void OnEquip()
	{
		base.OnEquip();
		Part.PlayerMover.PSM.ChargeAttackState.ChangeID("charge_attack_counter_upgrade");
		Part.PlayerMover.PSM.SpecialParryState.ChangeID("heat_parry_counter_upgrade");
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		Part.PlayerMover.PSM.ChargeAttackState.ChangeID("charge_attack_counter");
		Part.PlayerMover.PSM.SpecialParryState.ChangeID("heat_parry_counter");
	}
}
