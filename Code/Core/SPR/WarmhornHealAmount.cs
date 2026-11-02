using StaticData;

namespace SPR;

public class WarmhornHealAmount : Stat
{
	public bool CanHealInstantly { get; private set; }

	protected override bool UsingDecimal => false;

	public WarmhornHealAmount(float initial, Difficulty difficulty)
		: base(StatType.WarmhornHealAmount, initial, difficulty)
	{
		CanHealInstantly = true;
	}

	public void SetHealInstant(bool value)
	{
		CanHealInstantly = value;
	}
}
