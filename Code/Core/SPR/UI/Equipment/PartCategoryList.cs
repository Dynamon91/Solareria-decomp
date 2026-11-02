using System;
using System.Collections.Generic;
using SPR.Save;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SPR.UI.Equipment;

public class PartCategoryList : CategoryList
{
	[Header("Equipment Category List")]
	[SerializeField]
	private LocaleText leftLocaleText;

	[SerializeField]
	private LocaleText rightLocaleText;

	private readonly Dictionary<PartType, Action> notiHandlers = new Dictionary<PartType, Action>();

	public override void OnActivate(bool isRest)
	{
		SetArrowLocale(isRest);
	}

	private void SetArrowLocale(bool isRest)
	{
		if (isRest)
		{
			leftLocaleText.SetLocaleKey("UI_KeyGuide_PreviousCategoryUI");
			rightLocaleText.SetLocaleKey("UI_KeyGuide_NextCategoryUI");
			leftLocaleText.SetData("UI_KeyGuide_PreviousCategoryUI");
			rightLocaleText.SetData("UI_KeyGuide_NextCategoryUI");
		}
		else
		{
			leftLocaleText.SetLocaleKey("UI_KeyGuide_SubPreviousCategory");
			rightLocaleText.SetLocaleKey("UI_KeyGuide_SubNextCategory");
			leftLocaleText.SetData("UI_KeyGuide_SubPreviousCategory");
			rightLocaleText.SetData("UI_KeyGuide_SubNextCategory");
		}
	}

	protected override void SubscribeNotiCondition(UICategory category, int index)
	{
		WanderingData wanderingData = Game.Instance.SaveData.WanderingData;
		PartType partType = (PartType)index;
		if (notiHandlers.TryGetValue(partType, out var value))
		{
			wanderingData.OnUnwatchedPartDataChanged -= value;
		}
		Action action = delegate
		{
			category.SetNotiIcon(wanderingData.HasUnwatchedPart(partType));
		};
		action();
		wanderingData.OnUnwatchedPartDataChanged += action;
		notiHandlers[partType] = action;
	}

	protected override IEnumerable<(EventTriggerType type, Action<BaseEventData> action)> EventActions(int index)
	{
		yield return (type: EventTriggerType.PointerEnter, action: null);
	}
}
