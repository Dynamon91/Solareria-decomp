using System;
using UnityEngine;

[Serializable]
public class EnemyWaterData
{
	[field: SerializeField]
	public string ID { get; private set; }

	[field: SerializeField]
	public int Duration { get; private set; }
}
