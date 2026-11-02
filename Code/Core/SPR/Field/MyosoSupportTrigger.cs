using System;
using UnityEngine;

namespace SPR.Field;

[Serializable]
public struct MyosoSupportTrigger
{
	[SerializeField]
	private int wave;

	[SerializeField]
	private MyosoTiming[] timing;

	public int Wave => wave;

	public MyosoTiming[] Timing => timing;
}
