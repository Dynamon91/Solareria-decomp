using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.EventSystems;

namespace SPR.UI.Title;

public class TitleCategoryList : CategoryList
{
	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass1_0
	{
		public TitlePanel titlePanel;

		public TitleCategory category;

		internal void _003CEventActions_003Eb__0(BaseEventData _)
		{
			titlePanel.FocusCategory(category);
		}

		internal void _003CEventActions_003Eb__1(BaseEventData _)
		{
			titlePanel.SelectChoice(category);
		}
	}

	protected override void SubscribeNotiCondition(UICategory category, int index)
	{
	}

	protected override IEnumerable<(EventTriggerType type, Action<BaseEventData> action)> EventActions(int index)
	{
		new _003C_003Ec__DisplayClass1_0();
		yield break;
	}
}
