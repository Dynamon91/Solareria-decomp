using System.Collections.Generic;
using Rewired;
using SPR.Library;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Option;

public class OptionSettingPanel : OptionCategoryPanel, IControllerEventUpdate
{
	[SerializeField]
	private ScrollBoard scrollBoard;

	[SerializeField]
	private List<SettingItem> settingItems;

	[SerializeField]
	private List<SettingItem> gamepadSettingItems;

	private readonly List<int> disabledIndex = new List<int>();

	private SettingItem currentSettingItem;

	protected override int DefaultIndex
	{
		get
		{
			if (Game.FieldID.SectorName == SectorName.None)
			{
				return 1;
			}
			return 0;
		}
	}

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public override string ControllerName()
	{
		return "Option Setting Panel";
	}

	public override void Initialize()
	{
		base.Initialize();
		if (SteamUtils.IsSteamRunningOnSteamDeck())
		{
			disabledIndex.Add(3);
		}
	}

	protected override void OnActivate()
	{
		RewiredHelper.RegisterControllerChangeEvent(this);
		base.OnActivate();
		int item = settingItems.FindIndex((SettingItem settingItem2) => settingItem2 is SettingDifficultyPicker);
		if (Game.FieldID.SectorName == SectorName.None)
		{
			if (!disabledIndex.Contains(item))
			{
				disabledIndex.Add(item);
			}
			GetCategoryTransform(settingItems[0]).gameObject.SetActive(value: false);
		}
		else
		{
			disabledIndex.Remove(item);
			GetCategoryTransform(settingItems[0]).gameObject.SetActive(value: true);
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)GetCategoryTransform(settingItems[0]).parent);
		for (int num = 0; num < settingItems.Count; num++)
		{
			settingItems[num].CanvasGroup.alpha = (disabledIndex.Contains(num) ? 0.2f : 1f);
			SettingItem settingItem = settingItems[num];
			settingItem.Synchronize();
			settingItem.OnFocus(num == base.VerticalIndex);
		}
		bool flag = RewiredHelper.Gamepad != null;
		foreach (SettingItem gamepadSettingItem in gamepadSettingItems)
		{
			gamepadSettingItem.CanvasGroup.alpha = (flag ? 1f : 0.2f);
		}
		currentSettingItem = settingItems[base.VerticalIndex];
		scrollBoard.ScrollIfTargetIsOutOfViewport(GetCategoryTransform(currentSettingItem), 0f, instant: true);
		SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Confirm");
	}

	protected override void OnDeactivate()
	{
		RewiredHelper.RemoveControllerChangeEvent(this);
		base.OnDeactivate();
		SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Confirm");
	}

	protected override void VerticalControl(int vertical)
	{
		SettingItem settingItem = currentSettingItem;
		currentSettingItem = settingItems[base.VerticalIndex];
		settingItem.OnFocus(value: false);
		currentSettingItem.OnFocus(value: true);
		scrollBoard.ScrollIfTargetIsOutOfViewport(GetCategoryTransform(currentSettingItem));
	}

	protected override void HorizontalControl(int horizontal)
	{
		currentSettingItem.Control(horizontal, 0);
	}

	protected override int GetNextIndex(int current, int delta)
	{
		int num = settingItems.Count - 1;
		int num2 = Mathf.Clamp(current + delta, 0, num);
		if (disabledIndex.Contains(num2))
		{
			int num3 = ((delta > 0) ? (num2 + ((num2 < num) ? 1 : (-1))) : ((delta >= 0) ? current : (num2 + ((num2 <= 0) ? 1 : (-1)))));
			num2 = num3;
		}
		return Mathf.Clamp(num2, 0, num);
	}

	protected override void OnConfirm()
	{
		currentSettingItem.OnConfirm();
	}

	public void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
		bool flag = RewiredHelper.Gamepad != null;
		foreach (SettingItem gamepadSettingItem in gamepadSettingItems)
		{
			gamepadSettingItem.CanvasGroup.alpha = (flag ? 1f : 0.2f);
		}
	}

	private RectTransform GetCategoryTransform(SettingItem settingItem)
	{
		return (RectTransform)settingItem.transform.parent.parent;
	}
}
