using System;
using SPR;
using UnityEngine;

[Serializable]
public struct PowerByMass
{
	[field: SerializeField]
	public CreatureMass Mass { get; private set; }

	[field: SerializeField]
	public int KnockBackPower { get; private set; }

	[field: SerializeField]
	public int DashKnockBackPower { get; private set; }
}
