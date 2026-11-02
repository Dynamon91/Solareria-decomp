using System;
using UnityEngine;

namespace SPR;

[Serializable]
public struct MinMax
{
	[SerializeField]
	public float min;

	[SerializeField]
	public float max;

	public MinMax(float min, float max)
	{
		this.min = min;
		this.max = max;
	}
}
