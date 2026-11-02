using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CreditDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<CreditData> CreditDataList { get; private set; }
}
