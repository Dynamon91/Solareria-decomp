namespace SPR;

public class WeaponDefaultPart : SimpleStatEffectPart
{
	public override void OnEquip()
	{
		base.OnEquip();
		float defaultDamageMultiplier = 1f + base.PartData.PartEffectDatasForEffects[1].Rates[base.Level] / 100f;
		Part.Player.PlayerStat.SetDefaultDamageMultiplier(defaultDamageMultiplier);
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		Part.Player.PlayerStat.SetDefaultDamageMultiplier(1f);
	}
}
