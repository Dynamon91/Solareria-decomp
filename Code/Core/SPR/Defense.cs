using StaticData;

namespace SPR;

public class Defense : Stat
{
	public float Rate => base.Max / 100f;

	public Defense(float initial, Difficulty difficulty)
		: base(StatType.Defense, initial, difficulty)
	{
	}
}
