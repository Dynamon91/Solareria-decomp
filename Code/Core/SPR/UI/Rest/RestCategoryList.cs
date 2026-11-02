using System;
using System.Collections.Generic;
using System.Linq;
using SPR.Save;
using UnityEngine.EventSystems;

namespace SPR.UI.Rest;

public class RestCategoryList : CategoryList
{
	private readonly Dictionary<RestCategory, Action> notiHandlers = new Dictionary<RestCategory, Action>();

	protected override void SubscribeNotiCondition(UICategory category, int index)
	{
		PlayerData playerData = Game.Instance.SaveData.PlayerData;
		if (index == 0 || index == 3)
		{
			return;
		}
		if (notiHandlers.TryGetValue((RestCategory)index, out var value))
		{
			switch (index)
			{
			case 1:
				playerData.OnPartAcquiredOrEquipped -= value;
				break;
			case 2:
				playerData.OnTotalTraitPointChanged -= value;
				playerData.OnCardLevelChanged -= value;
				break;
			}
		}
		Action action = null;
		switch (index)
		{
		case 1:
			action = delegate
			{
				bool notiIcon = false;
				foreach (PartType partType in Enum.GetValues(typeof(PartType)))
				{
					if (!playerData.EquippedPartList.Any((PlayerData.PartData data) => data.PartType == partType) && playerData.AcquiredPartList.Any((PlayerData.PartData data) => data.PartType == partType))
					{
						notiIcon = true;
						break;
					}
				}
				category.SetNotiIcon(notiIcon);
			};
			action();
			playerData.OnPartAcquiredOrEquipped += action;
			break;
		case 2:
			action = delegate
			{
				category.SetNotiIcon(playerData.AvailableTraitPoint > 0);
			};
			action();
			playerData.OnTotalTraitPointChanged += action;
			playerData.OnCardLevelChanged += action;
			break;
		}
		notiHandlers[(RestCategory)index] = action;
	}

	protected override IEnumerable<(EventTriggerType type, Action<BaseEventData> action)> EventActions(int index)
	{
		yield return (type: EventTriggerType.PointerEnter, action: null);
	}
}
