using StaticData;

namespace SPR;

public class CollectingPower : Stat
{
	public float Rate => base.Max / 100f;

	protected override bool UsingPercent => true;

	public CollectingPower(float initial, Difficulty difficulty)
		: base(StatType.CollectingPower, initial, difficulty)
	{
	}
}
