using System;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class BossData
{
	[SerializeField]
	private BossName name;

	[SerializeField]
	private int count;

	[SerializeField]
	private float clearTime;

	public BossName Name => name;

	public int Count => count;

	public float ClearTime => clearTime;

	public BossData(BossName name)
	{
		this.name = name;
	}

	public void AddCount()
	{
		count++;
	}

	public bool TryBreakClearRecord(float time)
	{
		if (clearTime > 0f)
		{
			if (time <= clearTime)
			{
				clearTime = time;
				return true;
			}
			return false;
		}
		clearTime = time;
		return true;
	}
}
