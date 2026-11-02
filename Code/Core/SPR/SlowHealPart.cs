namespace SPR;

public class SlowHealPart : WarmhornPart
{
	private StatEffect statEffect;

	private int additionalHealAmount;

	public int HealDuration { get; private set; }

	public int HealCycle { get; private set; }

	public override void OnEquip()
	{
		base.OnEquip();
		PartEffectData data = base.PartData.PartEffectDatasForEffects[1];
		statEffect = new StatEffect(data, base.Level);
		statEffect.Apply();
		data = base.PartData.PartEffectDatasForEffects[2];
		HealDuration = data.DurationFrames[base.Level];
		HealCycle = data.CycleFrame;
		Part.Player.PlayerStat.GetStat<WarmhornHealAmount>().SetHealInstant(value: false);
		Part.Player.PlayerStat.GetStat<WarmhornCount>().AddPartValue(-1f);
	}

	public override void Reset()
	{
		Part.Player.PlayerStat.GetStat<WarmhornCount>().CancelHeal();
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		statEffect.Clear();
		Part.Player.PlayerStat.GetStat<WarmhornHealAmount>().SetHealInstant(value: true);
		Part.Player.PlayerStat.GetStat<WarmhornCount>().AddPartValue(1f);
	}
}
