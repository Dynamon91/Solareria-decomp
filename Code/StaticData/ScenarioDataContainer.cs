using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ScenarioDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<ScenarioData> ScenarioDataList { get; private set; }
}
