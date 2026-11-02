using UnityEngine;

namespace SPR;

public class HealthOnKillPart : SimpleStatEffectPart, IKillEvent
{
	private float healthRate;

	public override void OnEquip()
	{
		base.OnEquip();
		healthRate = (float)(int)base.PartData.PartEffectDatasForEffects[1].Rates[base.Level] / 100f;
	}

	public void OnKill()
	{
		Health stat = Part.Player.PlayerStat.GetStat<Health>();
		int amount = Mathf.CeilToInt((float)stat.MaxHealth * healthRate);
		stat.AddHealth(amount, healInstantly: true);
	}
}
