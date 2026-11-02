using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TraitData
{
	[field: SerializeField]
	public int ID { get; private set; }

	[field: SerializeField]
	public string Name { get; private set; }

	[field: SerializeField]
	public int MaxLevel { get; private set; }

	[field: SerializeField]
	public int MaxRank { get; private set; }

	[field: SerializeField]
	public List<StatType> Stats { get; private set; }

	[field: SerializeField]
	public string SkillName { get; private set; }

	[field: SerializeField]
	public int SkillUnlockLevel { get; private set; }
}
