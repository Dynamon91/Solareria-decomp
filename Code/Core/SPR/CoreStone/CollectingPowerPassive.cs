using UnityEngine;

namespace SPR.CoreStone;

public class CollectingPowerPassive : CoreStonePassive
{
	private PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	protected override void ApplyFirstPassive()
	{
		Game.Instance.Player.mover.OnDie += AddExperienceOnDie;
	}

	protected override void ApplySecondPassive()
	{
		PlayerStat.GetStat<GoodsProtection>().AddTraitPassive(50f);
	}

	protected override void ResetFirstPassive()
	{
		Game.Instance.Player.mover.OnDie -= AddExperienceOnDie;
	}

	protected override void ResetSecondPassive()
	{
		PlayerStat.GetStat<GoodsProtection>().AddTraitPassive(-50f);
	}

	private void AddExperienceOnDie(IAttackSource source)
	{
		if (source is BossCharacter bossCharacter)
		{
			float num = Mathf.Clamp((float)(bossCharacter.MaxHealth - bossCharacter.Health) / (float)bossCharacter.MaxHealth, 0f, 0.7f);
			PlayerStat.AddExperience(Mathf.RoundToInt(num * ((float)bossCharacter.Exp * PlayerControlParameter.Instance.ExpRateOnKilledByBoss)));
		}
	}
}
