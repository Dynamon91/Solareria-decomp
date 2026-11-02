using System;
using UnityEngine;

[Serializable]
public class EnemyMultiplierData
{
	[field: SerializeField]
	public int ClearCount { get; private set; }

	[field: SerializeField]
	public float Health { get; private set; }

	[field: SerializeField]
	public float Attack { get; private set; }

	[field: SerializeField]
	public float Groggy { get; private set; }

	[field: SerializeField]
	public float GroggyMaxIncrease { get; private set; }
}
