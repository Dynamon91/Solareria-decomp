using StaticData;

namespace SPR;

public class Attack : Stat
{
	public Attack(float initial, Difficulty difficulty)
		: base(StatType.Attack, initial, difficulty)
	{
	}
}
