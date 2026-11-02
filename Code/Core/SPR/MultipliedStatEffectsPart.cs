using System.Collections.Generic;

namespace SPR;

public abstract class MultipliedStatEffectsPart : MultiStatEffectPart
{
	public override void OnEquip()
	{
		float multiplier = GetMultiplier();
		if (StatEffects == null)
		{
			StatEffects = new List<StatEffect>(base.PartData.PartEffectDatasForEffects.Count);
		}
		if (StatEffects.Count == 0)
		{
			foreach (PartEffectData partEffectDatasForEffect in base.PartData.PartEffectDatasForEffects)
			{
				MultipliedStatEffect item = new MultipliedStatEffect(partEffectDatasForEffect, base.Level, multiplier);
				StatEffects.Add(item);
			}
		}
		StatEffects.ForEach(delegate(StatEffect e)
		{
			e.Apply();
		});
	}

	protected abstract float GetMultiplier();
}
