using System;
using UnityEngine;

[Serializable]
public class HeatGaugeData
{
	[field: SerializeField]
	public string ID { get; private set; }

	[field: SerializeField]
	public int Gauge0 { get; private set; }

	[field: SerializeField]
	public int Gauge1 { get; private set; }

	[field: SerializeField]
	public int Gauge2 { get; private set; }

	[field: SerializeField]
	public int Gauge3 { get; private set; }

	[field: SerializeField]
	public int Gauge4 { get; private set; }

	[field: SerializeField]
	public int Gauge5 { get; private set; }
}
