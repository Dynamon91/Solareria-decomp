using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PartEffectData
{
	[field: SerializeField]
	public int ID { get; private set; }

	[field: SerializeField]
	public StatType StatType { get; private set; }

	[field: SerializeField]
	public List<float> Values { get; private set; }

	[field: SerializeField]
	public List<float> Rates { get; private set; }

	[field: SerializeField]
	public List<float> MaxValues { get; private set; }

	[field: SerializeField]
	public List<int> DurationFrames { get; private set; }

	[field: SerializeField]
	public int CycleFrame { get; private set; }

	[field: SerializeField]
	public List<float> Conditions { get; private set; }
}
