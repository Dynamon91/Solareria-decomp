using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DifficultyDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<DifficultyStatData> DifficultyStatDataList { get; private set; }
}
