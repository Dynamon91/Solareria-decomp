using StaticData;

namespace SPR;

public class GuardDamageReduceRate : Stat
{
	public float GuardDamageRate => 1f - base.Max / 100f;

	protected override bool UsingPercent => true;

	public GuardDamageReduceRate(float initial, Difficulty difficulty)
		: base(StatType.GuardDamage, initial, difficulty)
	{
	}
}
