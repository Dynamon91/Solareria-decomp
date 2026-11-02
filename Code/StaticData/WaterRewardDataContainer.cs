using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WaterRewardDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<SproutRewardData> WaterRewardDataList { get; private set; }
}
