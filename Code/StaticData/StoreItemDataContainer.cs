using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StoreItemDataContainer : ScriptableObject
{
	[field: SerializeField]
	public List<StoreItemData> LeafaDataList { get; private set; }

	[field: SerializeField]
	public List<StoreItemData> JimaDataList { get; private set; }

	[field: SerializeField]
	public List<StoreItemData> PwoongDataList { get; private set; }

	[field: SerializeField]
	public List<StoreItemData> CardamineDataList { get; private set; }

	[field: SerializeField]
	public List<StoreItemData> MuhaDataList { get; private set; }

	[field: SerializeField]
	public List<StoreItemData> TouristPotDataList { get; private set; }

	[field: SerializeField]
	public List<StoreItemData> GemExchangerDataList { get; private set; }
}
