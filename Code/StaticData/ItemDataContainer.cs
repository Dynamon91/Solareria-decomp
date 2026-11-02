using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<ItemData> ItemDataList { get; private set; }

	[field: SerializeField]
	public List<FoodData> FoodDataList { get; private set; }
}
