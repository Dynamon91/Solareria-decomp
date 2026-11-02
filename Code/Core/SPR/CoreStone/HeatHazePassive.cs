namespace SPR.CoreStone;

public class HeatHazePassive : CoreStonePassive
{
	private PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	protected override void ApplyFirstPassive()
	{
		PlayerStat.GetStat<ResidualRate>().AddTraitPassive(20f);
	}

	protected override void ApplySecondPassive()
	{
		PlayerStat.GetStat<ResidualRate>().AddTraitPassive(30f);
	}

	protected override void ResetFirstPassive()
	{
		PlayerStat.GetStat<ResidualRate>().AddTraitPassive(-20f);
	}

	protected override void ResetSecondPassive()
	{
		PlayerStat.GetStat<ResidualRate>().AddTraitPassive(-30f);
	}
}
