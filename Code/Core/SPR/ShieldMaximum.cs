using StaticData;
using UnityEngine;

namespace SPR;

public class ShieldMaximum : Stat
{
	private float Rate => base.Max / 100f;

	public int CurrentMaximum => Mathf.CeilToInt(Rate * (float)Stat.PlayerStat.GetStat<Health>().MaxHealthWithDark);

	protected override bool UsingPercent => true;

	public ShieldMaximum(float initial, Difficulty difficulty)
		: base(StatType.ShieldMaximum, initial, difficulty)
	{
	}

	protected override void OnMaxChanged()
	{
		Game.Instance.Player.mover.PlayerShield.UpdateShield();
	}
}
