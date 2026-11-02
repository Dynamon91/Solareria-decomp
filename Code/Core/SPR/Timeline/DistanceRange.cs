using System;
using UnityEngine;

namespace SPR.Timeline;

[Serializable]
public struct DistanceRange
{
	[SerializeField]
	public float from;

	[SerializeField]
	public float to;
}
