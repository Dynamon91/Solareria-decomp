using UnityEngine;

namespace SPR;

public class HealOverHealthPart : WarmhornPart
{
	private int overHealth;

	private int currentShield;

	public override void OnStartHeal()
	{
		int maxHealth = Part.Player.PlayerStat.GetStat<Health>().MaxHealth;
		int currentHealth = Part.Player.PlayerStat.GetStat<Health>().CurrentHealth;
		overHealth = (int)(Part.Player.PlayerStat.GetStat<WarmhornHealAmount>().Max - (float)(maxHealth - currentHealth));
	}

	public override void OnHeal()
	{
		if (overHealth > 0)
		{
			overHealth = Mathf.Clamp(overHealth, 0, Part.Player.PlayerStat.GetStat<Health>().MaxHealth - Part.PlayerMover.PlayerShield.Health);
			Part.PlayerMover.PlayerShield.AddShield(overHealth);
		}
	}
}
