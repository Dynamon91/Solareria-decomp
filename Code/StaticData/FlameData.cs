using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FlameData
{
	[field: SerializeField]
	public string ID { get; private set; }

	[field: SerializeField]
	public int Count { get; private set; }

	[field: SerializeField]
	public int DelayFrame { get; private set; }

	[field: SerializeField]
	public List<int> CycleFrames { get; private set; }
}
