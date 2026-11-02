using UnityEngine;

namespace SPR;

public class HealthAbsorbPart : WarmhornPart
{
	private float heatDecreaseRate;

	private int residualHeatAmount;

	public override void OnEquip()
	{
		base.OnEquip();
		heatDecreaseRate = base.PartData.PartEffectDatasForEffects[1].Rates[base.Level] / 100f;
		WarmhornHealAmount stat = Part.Player.PlayerStat.GetStat<WarmhornHealAmount>();
		residualHeatAmount = Mathf.CeilToInt(stat.Max * heatDecreaseRate);
		stat.AddPartRate(0f - heatDecreaseRate);
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		Part.Player.PlayerStat.GetStat<WarmhornHealAmount>().AddPartRate(heatDecreaseRate);
	}

	public override void OnHeal()
	{
		Part.Player.PlayerStat.ResidualHeat.AddAmount(residualHeatAmount);
	}
}
