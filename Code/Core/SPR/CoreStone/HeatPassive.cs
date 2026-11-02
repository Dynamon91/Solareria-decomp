namespace SPR.CoreStone;

public class HeatPassive : CoreStonePassive
{
	private static PlayerMover PlayerMover => Game.Instance.Player.mover;

	private static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	private static ResidualHeat ResidualHeat => Game.Instance.Player.PlayerStat.ResidualHeat;

	protected override void ApplyFirstPassive()
	{
		PlayerMover.SetSkillActive(typeof(SpecialParryInputItem), active: true);
	}

	protected override void ApplySecondPassive()
	{
		PlayerHeat.LastEmber.OnUse += Heal;
	}

	protected override void ResetFirstPassive()
	{
		PlayerMover.SetSkillActive(typeof(SpecialParryInputItem), active: false);
	}

	protected override void ResetSecondPassive()
	{
		PlayerHeat.LastEmber.OnUse -= Heal;
	}

	private void Heal()
	{
		ResidualHeat.Heal(ResidualHeat.Value);
		float num = (float)(PlayerStat.HeatLevel * 20) / 100f * (float)PlayerStat.GetStat<Health>().MaxHealth;
		PlayerStat.GetStat<Health>().AddHealth((int)num);
	}
}
