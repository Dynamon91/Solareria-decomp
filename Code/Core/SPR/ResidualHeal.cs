using StaticData;

namespace SPR;

public class ResidualHeal : Stat
{
	public ResidualHeal(float initial, Difficulty difficulty)
		: base(StatType.ResidualHeal, initial, difficulty)
	{
	}
}
