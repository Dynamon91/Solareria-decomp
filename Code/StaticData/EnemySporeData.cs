using System;
using UnityEngine;

[Serializable]
public class EnemySporeData
{
	[field: SerializeField]
	public string ID { get; private set; }

	[field: SerializeField]
	public int Count { get; private set; }

	[field: SerializeField]
	public int SpawnCount { get; private set; }
}
