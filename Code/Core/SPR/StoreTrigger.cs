using System;
using System.Collections.Generic;
using System.Linq;
using Library;

namespace SPR;

public class StoreTrigger : Trigger
{
	private enum Status
	{
		Remain,
		SoldOut,
		WholeRemain,
		WholeSoldOut
	}

	private readonly ActorName merchant;

	private readonly Status status;

	public StoreTrigger(string target, string value)
	{
		if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(value))
		{
			throw new FormatException("InteractCount Trigger에서 Target과 Value는 반드시 존재해야합니다. 현재 값 : " + target + ", " + value);
		}
		merchant = target.ToEnum<ActorName>();
		status = value.ToEnum<Status>();
	}

	public override bool CheckTrigger()
	{
		IReadOnlyList<StoreItemData> storeItemDataList = Util.GetStoreItemDataList(merchant);
		List<string> list = storeItemDataList.Select((StoreItemData data) => data.ID).ToList();
		List<string> list2 = (from data in storeItemDataList
			where Trigger.CheckTrigger(data.AvailableTrigger)
			select data.ID).ToList();
		foreach (string item in from data in Trigger.SaveData.WorldData.StoreData.GetSoldItemIDList(merchant)
			select data.ID)
		{
			list.Remove(item);
			list2.Remove(item);
		}
		return status switch
		{
			Status.Remain => list2.Count > 0, 
			Status.SoldOut => list2.Count == 0, 
			Status.WholeRemain => list.Count > 0, 
			Status.WholeSoldOut => list.Count == 0, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}
}
