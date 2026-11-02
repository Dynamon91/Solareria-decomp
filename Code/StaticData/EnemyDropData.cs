using System;
using UnityEngine;

[Serializable]
public class EnemyDropData
{
	[field: SerializeField]
	public string ID { get; protected set; }

	[field: SerializeField]
	public int Rate { get; protected set; }

	[field: SerializeField]
	public int CrackedSolinShard { get; protected set; }

	[field: SerializeField]
	public int HardSolinShard { get; protected set; }

	[field: SerializeField]
	public int SuperiorSolinShard { get; protected set; }

	[field: SerializeField]
	public int CrackedSolinMass { get; protected set; }

	[field: SerializeField]
	public int HardSolinMass { get; protected set; }

	[field: SerializeField]
	public int SuperiorSolinMass { get; protected set; }
}
