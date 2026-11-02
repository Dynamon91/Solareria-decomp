using System;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class CreatureData
{
	[SerializeField]
	private string name;

	[SerializeField]
	private int count;

	[SerializeField]
	private int pkCount;

	public string Name => name;

	public int Count => count;

	public int PkCount => pkCount;

	public CreatureData(string name)
	{
		this.name = name;
	}

	public void AddDeath()
	{
		count++;
	}

	public void AddPlayerKill()
	{
		pkCount++;
	}
}
