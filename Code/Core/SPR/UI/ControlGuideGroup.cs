using System.Collections.Generic;
using DG.Tweening;
using Library;
using Rewired;
using SPR.Library;
using SPR.UI.Equipment;
using SPR.UI.Map;
using SPR.UI.Menu;
using SPR.UI.Trait;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class ControlGuideGroup : MonoBehaviour, IControllerEventUpdate, ILayoutRebuildParent
{
	[SerializeField]
	private List<GameObject> groups;

	[SerializeField]
	private List<LocaleText> mapTeleportGroup;

	private GameObject currentGroup;

	private CanvasGroup canvasGroup;

	private int? rebuildLeftChildren;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private void Awake()
	{
		RewiredHelper.RegisterControllerChangeEvent(this);
	}

	public void Change(MenuCategory category)
	{
		switch (category)
		{
		case MenuCategory.Status:
			ActiveGroup(0);
			break;
		case MenuCategory.Trait:
			ActiveGroup(1);
			break;
		case MenuCategory.Equipment:
			ActiveGroup(2);
			break;
		case MenuCategory.Inventory:
			ActiveGroup(3);
			break;
		case MenuCategory.Memory:
			ActiveGroup(4);
			break;
		case MenuCategory.Map:
			ActiveGroup(5);
			break;
		case MenuCategory.System:
			ActiveGroup(6);
			break;
		}
	}

	public void ActiveTraitDetail(bool active)
	{
		currentGroup?.SetActive(value: false);
	}

	public void ActivateTrait(SPR.UI.Trait.Mode mode)
	{
		ActivateGuides(MenuCategory.Trait, (int)mode);
	}

	public void ActivateMap(SPR.UI.Map.Mode mode)
	{
		ActivateGuides(MenuCategory.Map, (int)mode);
	}

	public void ActivateEquipment(SPR.UI.Equipment.Mode mode)
	{
		ActivateGuides(MenuCategory.Equipment, (int)mode, -1);
	}

	public void ActivateGuides(MenuCategory category, int digit, int countOffset = 0)
	{
		Transform transform = groups[(int)category].transform;
		int childCount = transform.childCount;
		for (int i = 0; i < childCount + countOffset; i++)
		{
			bool active = ((digit >> i) & 1) == 1;
			transform.GetChild(childCount - 1 - i).gameObject.SetActive(active);
		}
	}

	private void ActiveGroup(int index)
	{
		currentGroup?.SetActive(value: false);
		currentGroup = groups[index];
		currentGroup.SetActive(value: true);
		RebuildControlGuide();
		foreach (Transform item in currentGroup.transform)
		{
			if (item.TryGetComponent<KeyGuideItem>(out var component))
			{
				component.RegisterLayoutRebuildParent(this);
			}
		}
	}

	public void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
		if ((object)currentGroup != null)
		{
			RebuildControlGuide();
		}
	}

	public void DOFade(bool isActive, float duration, bool isRealTime = false)
	{
		CanvasGroup.DOKill();
		CanvasGroup.DOAlpha(isActive ? 1 : 0, duration).SetUpdate(isRealTime);
	}

	private void RebuildControlGuide()
	{
		foreach (Transform item in currentGroup.transform)
		{
			if (item.TryGetComponent<LocaleText>(out var component))
			{
				component.SetData();
			}
			if (item.TryGetComponent<ControlGuideIcon>(out var component2))
			{
				component2.UpdateIcon();
			}
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(currentGroup.GetComponent<RectTransform>());
	}

	public void TryRebuild()
	{
		int valueOrDefault = rebuildLeftChildren.GetValueOrDefault();
		if (!rebuildLeftChildren.HasValue)
		{
			valueOrDefault = currentGroup.transform.childCount;
			rebuildLeftChildren = valueOrDefault;
		}
		rebuildLeftChildren--;
		if (rebuildLeftChildren == 0)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(currentGroup.GetComponent<RectTransform>());
			rebuildLeftChildren = null;
		}
	}
}
