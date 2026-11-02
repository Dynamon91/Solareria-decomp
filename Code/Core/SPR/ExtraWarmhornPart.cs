namespace SPR;

public class ExtraWarmhornPart : SimpleStatEffectPart
{
	private int extraPotionCount;

	public override void OnEquip()
	{
		base.OnEquip();
		extraPotionCount = (int)base.PartData.PartEffectDatasForEffects.Find((PartEffectData x) => x.StatType == StatType.None).Values[base.Level];
		Part.Player.PlayerStat.GetStat<WarmhornCount>().AddPartValue(extraPotionCount);
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		Part.Player.PlayerStat.GetStat<WarmhornCount>().AddPartValue(-extraPotionCount);
	}
}
