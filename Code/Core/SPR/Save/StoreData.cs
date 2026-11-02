using System;
using System.Collections.Generic;
using SPR.UI.Store;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class StoreData
{
	[Serializable]
	public class SoldItemData
	{
		[SerializeField]
		private StoreType storeType;

		[SerializeField]
		private List<StoreItemData> soldItemIDList;

		public StoreType StoreType => storeType;

		public IReadOnlyList<StoreItemData> SoldItemIDList => soldItemIDList;

		public SoldItemData(StoreType storeType)
		{
			this.storeType = storeType;
			soldItemIDList = new List<StoreItemData>();
		}

		public void AddSoldItem(StoreItemData data)
		{
			soldItemIDList.Add(data);
		}

		internal void RemoveSoldItem(StoreItemData data)
		{
			soldItemIDList.Remove(data);
		}
	}

	[SerializeField]
	private List<SoldItemData> soldItemDatas;

	public StoreData()
	{
		soldItemDatas = new List<SoldItemData>();
	}

	public IReadOnlyList<StoreItemData> GetSoldItemIDList(StoreType storeType)
	{
		return GetOrAddSoldItemData(storeType).SoldItemIDList;
	}

	public IReadOnlyList<StoreItemData> GetSoldItemIDList(ActorName actorName)
	{
		return GetSoldItemIDList(actorName switch
		{
			ActorName.Leafa => StoreType.Restaurant, 
			ActorName.Jima => StoreType.Peddler, 
			ActorName.Pwoong => StoreType.Vendor, 
			ActorName.Cardamine => StoreType.Artifacts, 
			ActorName.TouristPot => StoreType.TouristPot, 
			ActorName.Muha => StoreType.Painter, 
			_ => StoreType.None, 
		});
	}

	public void AddSoldItem(StoreType storeType, StoreItemData data)
	{
		GetOrAddSoldItemData(storeType).AddSoldItem(data);
	}

	private SoldItemData GetOrAddSoldItemData(StoreType storeType)
	{
		foreach (SoldItemData soldItemData2 in soldItemDatas)
		{
			if (soldItemData2.StoreType == storeType)
			{
				return soldItemData2;
			}
		}
		SoldItemData soldItemData = new SoldItemData(storeType);
		soldItemDatas.Add(soldItemData);
		return soldItemData;
	}

	internal void RemoveSoldItemData(StoreType storeType, List<StoreItemData> removeData)
	{
		SoldItemData orAddSoldItemData = GetOrAddSoldItemData(storeType);
		foreach (StoreItemData removeDatum in removeData)
		{
			orAddSoldItemData.RemoveSoldItem(removeDatum);
		}
	}
}
