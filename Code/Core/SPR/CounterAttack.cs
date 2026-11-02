using StaticData;

namespace SPR;

public class CounterAttack : Stat
{
	public float Rate => 1f + base.Max / 100f;

	protected override bool UsingPercent => true;

	public CounterAttack(float initial, Difficulty difficulty)
		: base(StatType.CounterAttack, initial, difficulty)
	{
	}
}
