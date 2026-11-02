using System;
using UnityEngine;

[Serializable]
public class MapNoticeData
{
	[field: SerializeField]
	public string ID { get; private set; }

	[field: SerializeField]
	public string TargetID { get; private set; }
}
