using System;
using Library;
using SPR.Save;

namespace SPR;

public class FightCountTrigger : Trigger
{
	private enum Operator
	{
		None,
		Clear,
		NotClear
	}

	private readonly string enemy;

	private readonly int num;

	private readonly Operator cleared;

	public FightCountTrigger(string target, string value)
	{
		if (target == "")
		{
			throw new FormatException("FightCount Trigger에서 해당 적의 ID를 target값으로 반드시 사용해야 합니다!");
		}
		enemy = target;
		if (value.TryParseInt(out num) || Enum.TryParse<Operator>(value, out cleared))
		{
			return;
		}
		throw new FormatException("FightCount Trigger에서 value 값을 처리할 수 없습니다! (" + value + ")");
	}

	public override bool CheckTrigger()
	{
		try
		{
			BossName name = enemy.ToEnum<BossName>();
			BossData data;
			return cleared switch
			{
				Operator.None => Trigger.SaveData.EnemyFightData.TryGetBossData(name, out data) && data.Count >= num, 
				Operator.Clear => Trigger.SaveData.EnemyFightData.IsBossCleared(name), 
				Operator.NotClear => !Trigger.SaveData.EnemyFightData.IsBossCleared(name), 
				_ => throw new ArgumentOutOfRangeException(), 
			};
		}
		catch (FormatException)
		{
			CreatureData data2;
			return Trigger.SaveData.EnemyFightData.TryGetCreatureData(enemy, out data2) && data2.Count >= num;
		}
	}
}
