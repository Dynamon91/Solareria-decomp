using StaticData;

namespace SPR;

public class CollectingRange : Stat
{
	public float Rate => base.Max / 100f;

	public float Distance => 10f * Rate;

	protected override bool UsingPercent => true;

	public CollectingRange(float initial, Difficulty difficulty)
		: base(StatType.CollectingRange, initial, difficulty)
	{
	}
}
