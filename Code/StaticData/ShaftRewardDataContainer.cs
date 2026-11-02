using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShaftRewardDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<SproutRewardData> ShaftRewardDataList { get; private set; }
}
