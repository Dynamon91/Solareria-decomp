using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FoodData
{
	[field: SerializeField]
	public string ID { get; private set; }

	[field: SerializeField]
	public List<StatType> StatTypes { get; private set; }

	[field: SerializeField]
	public List<float> StatValues { get; private set; }
}
