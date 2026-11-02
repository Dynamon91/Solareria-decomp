using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyData
{
	[field: SerializeField]
	public string ID { get; protected set; }

	[field: SerializeField]
	public float Damage { get; protected set; }

	[field: SerializeField]
	public float DarkDamage { get; protected set; }

	[field: SerializeField]
	public AttackDirection AttackDirection { get; protected set; }

	[field: SerializeField]
	public List<HitType> HitTypes { get; protected set; }

	[field: SerializeField]
	public List<AttackAttribute> AttackAttributes { get; protected set; }

	[field: SerializeField]
	public List<float> KnockBack { get; protected set; }

	[field: SerializeField]
	public List<float> GuardKnockBack { get; protected set; }

	[field: SerializeField]
	public List<float> ParryKnockBack { get; protected set; }
}
