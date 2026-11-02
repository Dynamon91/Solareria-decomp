using System;
using UnityEngine;

namespace SPR.Field;

[Serializable]
public struct MyosoTiming
{
	[SerializeField]
	private TargetType targetType;

	[SerializeField]
	private int timing;

	public TargetType TargetType => targetType;

	public int Timing => timing;
}
