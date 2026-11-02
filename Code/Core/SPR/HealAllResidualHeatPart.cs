namespace SPR;

public class HealAllResidualHeatPart : WarmhornPart
{
	public override void OnHeal()
	{
		ResidualHeat residualHeat = Part.Player.PlayerStat.ResidualHeat;
		residualHeat.Heal(residualHeat.Value);
	}
}
