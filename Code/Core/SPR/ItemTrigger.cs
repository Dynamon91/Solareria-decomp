using System;
using System.Collections.Generic;
using System.Linq;
using Library;

namespace SPR;

public class ItemTrigger : Trigger
{
	private readonly string itemId;

	private readonly int shouldHaveNum;

	private readonly Dictionary<string, int> itemDictionary;

	public ItemTrigger(string target, string value)
	{
		if (target == "")
		{
			throw new FormatException("Item Trigger에서 해당 아이템의 ID를 target값으로 반드시 사용해야 합니다!");
		}
		List<string> list;
		List<int> list2;
		if (Enum.TryParse<ItemType>(target, out var itemType))
		{
			list = (from itemData in DataManager.Instance.GetAllItemDataList()
				where itemData.ItemType == itemType
				select itemData.ID).ToList();
			list2 = Enumerable.Repeat(value.ToInt(), list.Count).ToList();
		}
		else
		{
			list = Split(target).ToList();
			list2 = (from s in Split(value)
				select s.ToInt()).ToList();
		}
		if (list.Count != list2.Count)
		{
			throw new FormatException("Item Trigger에서 target의 개수와 value의 개수가 일치하지 않습니다!\ntarget = " + target + "\nvalue = " + value);
		}
		itemDictionary = new Dictionary<string, int>();
		for (int num = 0; num < list.Count; num++)
		{
			itemDictionary.Add(list[num], list2[num]);
		}
	}

	public override bool CheckTrigger()
	{
		if (itemDictionary == null || itemDictionary.Count == 0)
		{
			return true;
		}
		return itemDictionary.All((KeyValuePair<string, int> kvp) => Trigger.SaveData.PlayerData.GetAcquiredItemCount(kvp.Key) >= kvp.Value);
	}

	private static IEnumerable<string> Split(string value)
	{
		return value.Replace(" ", "").Split(',');
	}
}
