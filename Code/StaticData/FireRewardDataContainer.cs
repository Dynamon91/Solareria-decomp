using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FireRewardDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<SproutRewardData> FireRewardDataList { get; private set; }
}
