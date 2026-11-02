namespace SPR;

public class AllStatNonHeatPart : MultipliedStatEffectsPart, IHeatUnlockEvent
{
	public override void OnEquip()
	{
		base.OnEquip();
		PlayerHeat.SetGainSpeed(0f);
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		PlayerHeat.SetGainSpeed(1f);
	}

	protected override float GetMultiplier()
	{
		return PlayerStat.HeatMaxLevel;
	}

	public void OnUnlockHeat(int maxLevel)
	{
		OnUnequip();
		OnEquip();
	}
}
