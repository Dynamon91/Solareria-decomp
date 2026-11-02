using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WindRewardDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<SproutRewardData> WindRewardDataList { get; private set; }
}
