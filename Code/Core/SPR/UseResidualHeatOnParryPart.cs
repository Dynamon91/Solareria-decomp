using System.Collections.Generic;
using Library;

namespace SPR;

public class UseResidualHeatOnParryPart : SimpleStatEffectPart, IResidualHeatChangedEvent
{
	private List<float> appliedStatValues;

	public override void OnEquip()
	{
		base.OnEquip();
		ApplyStat();
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		ClearStat();
	}

	private void ApplyStat()
	{
		int count = base.PartData.PartEffectDatasForEffects.Count;
		if (appliedStatValues == null)
		{
			appliedStatValues = new List<float>(count);
		}
		for (int i = 1; i < count; i++)
		{
			PartEffectData partEffectData = base.PartData.PartEffectDatasForEffects[i];
			float num = (float)Part.Player.PlayerStat.ResidualHeat.Value * partEffectData.Rates[base.Level] / 100f;
			Part.Player.PlayerStat.GetStat(partEffectData.StatType).AddPartValue(num);
			appliedStatValues.Add(num);
		}
	}

	private void ClearStat()
	{
		if (!appliedStatValues.IsNullOrEmpty())
		{
			int count = base.PartData.PartEffectDatasForEffects.Count;
			for (int i = 1; i < count; i++)
			{
				PartEffectData partEffectData = base.PartData.PartEffectDatasForEffects[i];
				Part.Player.PlayerStat.GetStat(partEffectData.StatType).AddPartValue(0f - appliedStatValues[i - 1]);
			}
			appliedStatValues.Clear();
		}
	}

	public void OnResidualHeatChanged(int value)
	{
		ClearStat();
		ApplyStat();
	}
}
