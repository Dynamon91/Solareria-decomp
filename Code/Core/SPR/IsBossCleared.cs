using Library;

namespace SPR;

public class IsBossCleared : Trigger
{
	private readonly BossName bossName;

	public IsBossCleared(string target, string value)
	{
		bossName = value.ToEnum<BossName>();
	}

	public override bool CheckTrigger()
	{
		return Trigger.SaveData.EnemyFightData.IsBossCleared(bossName);
	}
}
