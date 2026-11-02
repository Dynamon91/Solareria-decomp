using StaticData;

namespace SPR;

public class ResidualRate : Stat
{
	public float Rate => base.Current / 100f;

	protected override bool UsingPercent => true;

	public ResidualRate(float initial, Difficulty difficulty)
		: base(StatType.ResidualRate, initial, difficulty)
	{
	}
}
