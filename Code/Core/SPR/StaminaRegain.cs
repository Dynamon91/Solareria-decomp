using StaticData;

namespace SPR;

public class StaminaRegain : Stat
{
	public StaminaRegain(float initial, Difficulty difficulty)
		: base(StatType.StaminaRegain, initial, difficulty)
	{
	}
}
