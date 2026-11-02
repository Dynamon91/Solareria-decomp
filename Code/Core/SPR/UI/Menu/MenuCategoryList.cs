using System;
using System.Collections.Generic;
using SPR.Save;
using UnityEngine.EventSystems;

namespace SPR.UI.Menu;

public class MenuCategoryList : CategoryList
{
	private readonly Dictionary<MenuCategory, Action> notiHandlers = new Dictionary<MenuCategory, Action>();

	public void SetCategoryActive(List<bool> actives)
	{
		for (int i = 0; i < categories.Count; i++)
		{
			categories[i].SetCategoryActive(actives[i]);
		}
	}

	protected override void SubscribeNotiCondition(UICategory category, int index)
	{
		WanderingData wanderingData = Game.Instance.SaveData.WanderingData;
		if (notiHandlers.TryGetValue((MenuCategory)index, out var value))
		{
			switch (index)
			{
			case 2:
				wanderingData.OnUnwatchedPartDataChanged -= value;
				break;
			case 3:
				wanderingData.OnUnwatchedInventoryDataChanged -= value;
				break;
			case 4:
				wanderingData.OnUnwatchedMemoryDataChanged -= value;
				break;
			case 5:
				wanderingData.OnUnwatchedIconDataChanged -= value;
				break;
			case 1:
				wanderingData.OnUnwatchedCoreStoneDataChanged -= value;
				break;
			}
		}
		Action action = null;
		switch (index)
		{
		case 2:
			action = delegate
			{
				category.SetNotiIcon(wanderingData.HasAnyUnwatchedPart());
			};
			action();
			wanderingData.OnUnwatchedPartDataChanged += action;
			break;
		case 3:
			action = delegate
			{
				category.SetNotiIcon(wanderingData.CountUnwatchedInventoryItems() > 0);
			};
			action();
			wanderingData.OnUnwatchedInventoryDataChanged += action;
			break;
		case 4:
			action = delegate
			{
				category.SetNotiIcon(wanderingData.CountUnwatchedMemories() > 0);
			};
			action();
			wanderingData.OnUnwatchedMemoryDataChanged += action;
			break;
		case 5:
			action = delegate
			{
				category.SetNotiIcon(wanderingData.CountUnwatchedIcons() > 0);
			};
			action();
			wanderingData.OnUnwatchedIconDataChanged += action;
			break;
		case 1:
			action = delegate
			{
				category.SetNotiIcon(wanderingData.HasUnwatchedCoreStone());
			};
			action();
			wanderingData.OnUnwatchedCoreStoneDataChanged += action;
			break;
		}
		notiHandlers[(MenuCategory)index] = action;
	}

	protected override IEnumerable<(EventTriggerType type, Action<BaseEventData> action)> EventActions(int index)
	{
		yield return (type: EventTriggerType.PointerEnter, action: null);
	}
}
