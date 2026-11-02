using System;
using System.Collections.Generic;
using SPR.Save;
using UnityEngine.EventSystems;

namespace SPR.UI.Memory;

public class MemoryCategoryList : CategoryList
{
	private readonly Dictionary<MemoryCategory, Action> notiHandlers = new Dictionary<MemoryCategory, Action>();

	protected override void SubscribeNotiCondition(UICategory category, int index)
	{
		WanderingData wanderingData = Game.Instance.SaveData.WanderingData;
		MemoryCategory memoryType = (MemoryCategory)index;
		if (notiHandlers.TryGetValue(memoryType, out var value))
		{
			wanderingData.OnUnwatchedMemoryDataChanged -= value;
		}
		Action action = delegate
		{
			category.SetNotiIcon(wanderingData.GetUnwatchedMemories(memoryType).Count > 0);
		};
		action();
		wanderingData.OnUnwatchedMemoryDataChanged += action;
		notiHandlers[memoryType] = action;
	}

	protected override IEnumerable<(EventTriggerType type, Action<BaseEventData> action)> EventActions(int index)
	{
		yield return (type: EventTriggerType.PointerEnter, action: null);
	}
}
