using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace SPR.UI.Option;

public class SettingCategoryList : CategoryList
{
	protected override void SubscribeNotiCondition(UICategory category, int index)
	{
	}

	protected override IEnumerable<(EventTriggerType type, Action<BaseEventData> action)> EventActions(int index)
	{
		yield return (type: EventTriggerType.PointerEnter, action: null);
	}
}
