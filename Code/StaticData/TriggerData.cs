using System;
using UnityEngine;

[Serializable]
public class TriggerData
{
	[field: SerializeField]
	public string ID { get; private set; }

	[field: SerializeField]
	public TriggerType Type1 { get; private set; }

	[field: SerializeField]
	public string Target1 { get; private set; }

	[field: SerializeField]
	public string Value1 { get; private set; }

	[field: SerializeField]
	public TriggerType Type2 { get; private set; }

	[field: SerializeField]
	public string Target2 { get; private set; }

	[field: SerializeField]
	public string Value2 { get; private set; }

	[field: SerializeField]
	public TriggerType Type3 { get; private set; }

	[field: SerializeField]
	public string Target3 { get; private set; }

	[field: SerializeField]
	public string Value3 { get; private set; }
}
