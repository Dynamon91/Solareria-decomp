using System;
using UnityEngine;

namespace SPR;

[Serializable]
public struct AttackTiming
{
	[SerializeField]
	private Zone zone;

	[SerializeField]
	private Timing timing;

	public Zone Zone => zone;

	public int Start => timing.Start;

	public int End => timing.End;

	public int Duration => timing.Duration;
}
