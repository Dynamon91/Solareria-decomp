using System;
using UnityEngine;

[Serializable]
public class EnemyBurnData
{
	[field: SerializeField]
	public string ID { get; private set; }

	[field: SerializeField]
	public int Damage { get; private set; }

	[field: SerializeField]
	public int CycleFrame { get; private set; }

	[field: SerializeField]
	public int Duration { get; private set; }
}
