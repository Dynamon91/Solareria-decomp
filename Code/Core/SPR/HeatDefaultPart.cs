using System;

namespace SPR;

public class HeatDefaultPart : SimpleStatEffectPart
{
	private Action healEvent;

	public override void OnEquip()
	{
		base.OnEquip();
		float rate = base.PartData.PartEffectDatasForEffects[1].Rates[base.Level] / 100f;
		Health health = Part.Player.PlayerStat.GetStat<Health>();
		healEvent = delegate
		{
			health.AddHealth((int)((float)health.MaxHealth * rate), healInstantly: true);
		};
		foreach (IHeatEffect heatEffectInputItem in Part.PlayerMover.PSM.HeatEffectInputItems)
		{
			heatEffectInputItem.OnUse += healEvent;
		}
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		foreach (IHeatEffect heatEffectInputItem in Part.PlayerMover.PSM.HeatEffectInputItems)
		{
			heatEffectInputItem.OnUse -= healEvent;
		}
	}
}
