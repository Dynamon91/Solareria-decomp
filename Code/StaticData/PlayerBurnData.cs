using System;
using UnityEngine;

[Serializable]
public class PlayerBurnData
{
	[field: SerializeField]
	public string ID { get; private set; }

	[field: SerializeField]
	public int CycleFrame { get; private set; }

	[field: SerializeField]
	public int Duration { get; private set; }
}
