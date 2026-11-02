using System.Collections.Generic;

namespace SPR;

public abstract class MultiStatEffectPart : Part
{
	protected List<StatEffect> StatEffects;

	public override void OnEquip()
	{
		if (StatEffects == null)
		{
			StatEffects = new List<StatEffect>(base.PartData.PartEffectDatasForEffects.Count);
		}
		if (StatEffects.Count == 0)
		{
			foreach (PartEffectData partEffectDatasForEffect in base.PartData.PartEffectDatasForEffects)
			{
				StatEffect item = new StatEffect(partEffectDatasForEffect, base.Level);
				StatEffects.Add(item);
			}
		}
		StatEffects.ForEach(delegate(StatEffect e)
		{
			e.Apply();
		});
	}

	public override void Reset()
	{
	}

	public override void OnUnequip()
	{
		StatEffects.ForEach(delegate(StatEffect e)
		{
			e.Clear();
		});
		StatEffects.Clear();
	}
}
