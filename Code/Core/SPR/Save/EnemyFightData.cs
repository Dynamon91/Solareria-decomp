using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class EnemyFightData
{
	[SerializeField]
	private List<CreatureData> creatures = new List<CreatureData>();

	[SerializeField]
	private List<BossData> bosses = new List<BossData>();

	public bool TryGetCreatureData(string name, out CreatureData data)
	{
		data = creatures.FirstOrDefault((CreatureData cd) => cd.Name == name);
		return data != null;
	}

	public int GetCreatureDeathCount(string name)
	{
		if (!TryGetCreatureData(name, out var data))
		{
			return 0;
		}
		return data.Count;
	}

	public int GetCreatureKillCount(string name)
	{
		if (!TryGetCreatureData(name, out var data))
		{
			return 0;
		}
		return data.PkCount;
	}

	public void AddCreatureDeath(string name)
	{
		GetCreatureData(name).AddDeath();
	}

	public void AddCreaturePlayerKill(string name)
	{
		GetCreatureData(name).AddPlayerKill();
	}

	private CreatureData GetCreatureData(string name)
	{
		if (!TryGetCreatureData(name, out var data))
		{
			data = new CreatureData(name);
			creatures.Add(data);
		}
		return data;
	}

	public bool TryGetBossData(BossName name, out BossData data)
	{
		data = bosses.FirstOrDefault((BossData bd) => bd.Name == name);
		return data != null;
	}

	public int GetBossTryCount(BossName name)
	{
		if (!TryGetBossData(name, out var data))
		{
			return 0;
		}
		return data.Count;
	}

	public void AddBossTry(BossName name)
	{
		GetBossData(name).AddCount();
	}

	public bool TryBreakBossClearRecord(BossName name, float time)
	{
		return GetBossData(name).TryBreakClearRecord(time);
	}

	public bool IsBossCleared(BossName name)
	{
		if (TryGetBossData(name, out var data))
		{
			return data.ClearTime > 0f;
		}
		return false;
	}

	public int BossClearCount()
	{
		int num = 0;
		foreach (BossData boss in bosses)
		{
			if (boss.ClearTime > 0f)
			{
				num++;
			}
		}
		return num;
	}

	private BossData GetBossData(BossName name)
	{
		if (!TryGetBossData(name, out var data))
		{
			data = new BossData(name);
			bosses.Add(data);
		}
		return data;
	}
}
