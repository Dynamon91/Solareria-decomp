using System.Collections.Generic;

namespace SPR;

public class UseResidualHeatOnPyronPart : SimpleStatEffectPart, IPyronEvent
{
	private int usage;

	public override void OnEquip()
	{
		base.OnEquip();
		PartEffectData partEffectData = base.PartData.PartEffectDatasForEffects[1];
		usage = (int)partEffectData.Values[base.Level];
	}

	public void OnPyronAttack(List<IDamageable> damaged, bool isHeavyPyron)
	{
		if (!isHeavyPyron)
		{
			return;
		}
		ResidualHeat residualHeat = Part.Player.PlayerStat.ResidualHeat;
		if (residualHeat.Value <= usage)
		{
			return;
		}
		int num = residualHeat.Value / usage;
		residualHeat.RemoveResidualHeat(num * usage);
		foreach (IDamageable item in damaged)
		{
			if (item is Enemy enemy)
			{
				for (int i = 0; i < num; i++)
				{
					HeatFlame.ShootFlame(new HeatFlame.FlameParameter(enemy, 0));
				}
			}
		}
	}
}
