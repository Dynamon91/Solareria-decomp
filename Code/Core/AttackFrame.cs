using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct AttackFrame
{
	[field: SerializeField]
	public int Predelay { get; private set; }

	[field: SerializeField]
	public int Action { get; private set; }

	[field: SerializeField]
	public int Postdelay { get; private set; }

	[field: SerializeField]
	public List<int> DamageFrames { get; private set; }

	public int All => Predelay + Action + Postdelay;

	public int ActionStart => Predelay + 1;

	public int ActionEnd => Predelay + Action;
}
