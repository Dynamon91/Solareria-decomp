using System;
using UnityEngine;

namespace SPR;

[Serializable]
public struct Timing
{
	[SerializeField]
	private int start;

	[SerializeField]
	private int duration;

	[ReadOnlyInspector]
	[SerializeField]
	private int end;

	public int Start => start;

	public int End => start + duration;

	public int Duration => duration;
}
