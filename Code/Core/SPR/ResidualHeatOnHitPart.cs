namespace SPR;

public class ResidualHeatOnHitPart : SimpleStatEffectPart, IDamagedEvent
{
	private float rate;

	public override void OnEquip()
	{
		base.OnEquip();
		rate = base.PartData.PartEffectDatasForEffects[1].Rates[base.Level] / 100f;
	}

	public void OnDamaged(IAttackSource source, int damage)
	{
		if (!Part.Player.mover.PSM.StateOption.HasFlag(StateOption.Guard))
		{
			Part.Player.PlayerStat.ResidualHeat.AddAmount((int)((float)damage * rate));
		}
	}
}
