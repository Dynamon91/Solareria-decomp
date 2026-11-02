using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerStatDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<FlameData> FlameDataList { get; private set; }

	[field: SerializeField]
	public List<PlayerBurnData> PlayerBurnDataList { get; private set; }

	[field: SerializeField]
	public List<SkillData> SkillDataList { get; private set; }

	[field: SerializeField]
	public List<TraitData> TraitDataList { get; private set; }
}
