using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MapNoticeDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<MapNoticeData> MapNoticeDataList { get; private set; }
}
