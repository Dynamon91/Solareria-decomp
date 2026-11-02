namespace SPR;

public class ResidualHeatOnParryPart : SimpleStatEffectPart, IParryEvent
{
	private int residualHeat;

	public override void OnEquip()
	{
		base.OnEquip();
		residualHeat = (int)base.PartData.PartEffectDatasForEffects[1].Values[base.Level];
		Part.Player.PlayerStat.ResidualHeat.BlockHealOnParry(block: true);
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		Part.Player.PlayerStat.ResidualHeat.BlockHealOnParry(block: false);
	}

	public void OnParry(Enemy enemy)
	{
		Part.Player.PlayerStat.ResidualHeat.AddAmount(residualHeat);
	}
}
