using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyStatData
{
	[field: SerializeField]
	public string ID { get; protected set; }

	[field: SerializeField]
	public int Exp { get; protected set; }

	[field: SerializeField]
	public int Money { get; protected set; }

	[field: SerializeField]
	public int HP { get; protected set; }

	[field: SerializeField]
	public int Stun { get; protected set; }

	[field: SerializeField]
	public List<int> Def { get; protected set; }

	[field: SerializeField]
	public int Phase2HP { get; protected set; }

	[field: SerializeField]
	public int Phase3HP { get; protected set; }

	[field: SerializeField]
	public int Duration { get; protected set; }

	[field: SerializeField]
	public int InitialMax { get; protected set; }

	[field: SerializeField]
	public int MaxIncrease { get; protected set; }

	[field: SerializeField]
	public float RecoveryPerFrame { get; protected set; }

	[field: SerializeField]
	public float StabilizeRecoveryPerFrame { get; protected set; }

	[field: SerializeField]
	public int StabilizeFrame { get; protected set; }

	[field: SerializeField]
	public float DamageMultiplier { get; protected set; }
}
