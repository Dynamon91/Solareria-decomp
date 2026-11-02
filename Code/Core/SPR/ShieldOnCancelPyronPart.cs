using UnityEngine;

namespace SPR;

public class ShieldOnCancelPyronPart : SimpleStatEffectPart
{
	private float rate;

	public override void OnEquip()
	{
		base.OnEquip();
		rate = base.PartData.PartEffectDatasForEffects[1].Rates[base.Level] / 100f;
		Part.PlayerMover.PSM.PyronRollingInputItem.SetCancelPyronAction(AddShield);
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		Part.PlayerMover.PSM.PyronRollingInputItem.SetCancelPyronAction(null);
	}

	private void AddShield()
	{
		int health = Mathf.CeilToInt((float)Part.Player.PlayerStat.GetStat<Health>().MaxHealth * rate);
		Part.Player.mover.PlayerShield.AddShield(health);
	}
}
