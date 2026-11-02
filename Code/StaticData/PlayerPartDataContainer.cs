using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerPartDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<PartData> PartDataList { get; private set; }

	[field: SerializeField]
	public List<PartEffectData> PartEffectDataList { get; private set; }
}
