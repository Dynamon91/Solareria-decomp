namespace SPR.CoreStone;

public class MysteryPassive : CoreStonePassive
{
	private PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	protected override void ApplyFirstPassive()
	{
		PlayerStat.GetStat<WarmhornHealAmount>().AddTraitPassive(2f);
	}

	protected override void ApplySecondPassive()
	{
		PlayerStat.GetStat<WarmhornHealAmount>().AddTraitPassive(3f);
	}

	protected override void ResetFirstPassive()
	{
		PlayerStat.GetStat<WarmhornHealAmount>().AddTraitPassive(-2f);
	}

	protected override void ResetSecondPassive()
	{
		PlayerStat.GetStat<WarmhornHealAmount>().AddTraitPassive(-3f);
	}
}
