using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BlacklistDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<BlacklistMemoryData> BlacklistMemoryDataList { get; private set; }
}
