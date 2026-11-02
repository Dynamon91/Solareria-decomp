using StaticData;

namespace SPR;

public class GroggyAttack : Stat
{
	public GroggyAttack(float initial, Difficulty difficulty)
		: base(StatType.GroggyAttack, initial, difficulty)
	{
	}
}
