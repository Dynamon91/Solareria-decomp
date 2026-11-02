using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class PartData
{
	[SerializeField]
	private List<int> effects;

	[field: SerializeField]
	public string ID { get; private set; }

	[field: SerializeField]
	public PartType PartType { get; private set; }

	public List<PartEffectData> PartEffectDatasForEffects => effects.Select((int x) => DataManager.Instance.GetPartEffectData(x)).ToList();

	[field: SerializeField]
	public int MaxLevel { get; private set; }

	[field: SerializeField]
	public List<int> UpgradeCosts { get; private set; }
}
