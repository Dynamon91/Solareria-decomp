using StaticData;

namespace SPR;

public class GoodsProtection : Stat
{
	public float LoseRate => 1f - base.Max / 100f;

	protected override bool UsingPercent => true;

	public GoodsProtection(float initial, Difficulty difficulty)
		: base(StatType.GoodsProtection, initial, difficulty)
	{
	}
}
