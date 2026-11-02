using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SporeRewardDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<SproutRewardData> SporeRewardDataList { get; private set; }
}
