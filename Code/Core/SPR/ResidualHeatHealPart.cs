namespace SPR;

public class ResidualHeatHealPart : SimpleStatEffectPart
{
	private int healAmount;

	private int cycleFrame;

	private int frameCount;

	private float healRate;

	public override void OnEquip()
	{
		base.OnEquip();
		PartEffectData partEffectData = base.PartData.PartEffectDatasForEffects[1];
		healAmount = (int)partEffectData.Values[base.Level];
		healRate = partEffectData.Rates[base.Level] / 100f;
		cycleFrame = partEffectData.CycleFrame;
		frameCount = 0;
	}

	public override void OnFixedUpdate()
	{
		if (Part.Player.PlayerStat.ResidualHeat.Value <= 0)
		{
			frameCount = 0;
			return;
		}
		frameCount++;
		if (frameCount > 0 && frameCount % cycleFrame == 0)
		{
			Part.Player.PlayerStat.ResidualHeat.Heal(healAmount, healRate);
		}
	}

	public override void Reset()
	{
		frameCount = 0;
	}
}
