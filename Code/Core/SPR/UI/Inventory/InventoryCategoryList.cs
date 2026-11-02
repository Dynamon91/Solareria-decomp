using System;
using System.Collections.Generic;
using System.Linq;
using SPR.Save;
using UnityEngine.EventSystems;

namespace SPR.UI.Inventory;

public class InventoryCategoryList : CategoryList
{
	private readonly Dictionary<InventoryCategory, Action> notiHandlers = new Dictionary<InventoryCategory, Action>();

	protected override void SubscribeNotiCondition(UICategory category, int index)
	{
		WanderingData wanderingData = Game.Instance.SaveData.WanderingData;
		List<ItemType> itemTypes = MenuInventoryPanel.GetItemTypesByCategory((InventoryCategory)index);
		if (notiHandlers.TryGetValue((InventoryCategory)index, out var value))
		{
			wanderingData.OnUnwatchedInventoryDataChanged -= value;
		}
		Action action = delegate
		{
			bool notiIcon = itemTypes.Any((ItemType itemType) => wanderingData.HasUnwatchedInventoryItem(itemType));
			category.SetNotiIcon(notiIcon);
		};
		action();
		wanderingData.OnUnwatchedInventoryDataChanged += action;
		notiHandlers[(InventoryCategory)index] = action;
	}

	protected override IEnumerable<(EventTriggerType type, Action<BaseEventData> action)> EventActions(int index)
	{
		yield return (type: EventTriggerType.PointerEnter, action: null);
	}
}
