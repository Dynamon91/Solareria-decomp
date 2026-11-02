namespace SPR.CoreStone;

public class WarmthPassive : CoreStonePassive
{
	private PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	protected override void ApplyFirstPassive()
	{
		PlayerStat.GetStat<ShieldMaximum>().AddTraitPassive(5f);
	}

	protected override void ApplySecondPassive()
	{
		PlayerStat.GetStat<ShieldMaximum>().AddTraitPassive(5f);
	}

	protected override void ResetFirstPassive()
	{
		PlayerStat.GetStat<ShieldMaximum>().AddTraitPassive(-5f);
	}

	protected override void ResetSecondPassive()
	{
		PlayerStat.GetStat<ShieldMaximum>().AddTraitPassive(-5f);
	}
}
