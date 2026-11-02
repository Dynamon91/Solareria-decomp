namespace SPR;

public class ChargeDamagePart : SimpleStatEffectPart, IChargeAttackEvent
{
	private float damageRate;

	public override void OnEquip()
	{
		base.OnEquip();
		damageRate = base.PartData.PartEffectDatasForEffects[1].Rates[base.Level] / 100f;
		Part.PlayerMover.PSM.BasicAttackInputItem.Active = false;
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		Part.PlayerMover.PSM.BasicAttackInputItem.Active = true;
	}

	public float GetDamageAdditionalRate(float staminaUsage)
	{
		return damageRate;
	}
}
