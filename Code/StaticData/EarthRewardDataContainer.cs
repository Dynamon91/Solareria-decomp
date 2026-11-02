using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EarthRewardDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<SproutRewardData> EarthRewardDataList { get; private set; }
}
