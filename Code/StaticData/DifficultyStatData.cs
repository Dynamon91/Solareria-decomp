using System;
using UnityEngine;

[Serializable]
public class DifficultyStatData
{
	[field: SerializeField]
	public string ID { get; private set; }

	[field: SerializeField]
	public float Story { get; private set; }

	[field: SerializeField]
	public float Easy { get; private set; }

	[field: SerializeField]
	public float Normal { get; private set; }

	[field: SerializeField]
	public float Main { get; private set; }
}
