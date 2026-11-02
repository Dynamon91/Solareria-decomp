using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TriggerDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<TriggerData> TriggerDataList { get; private set; }
}
