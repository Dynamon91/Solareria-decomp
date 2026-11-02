using System.Collections.Generic;
using Rewired;
using SPR.Library;
using UnityEngine;

namespace SPR.UI.Option;

public class OptionControlPanel : OptionCategoryPanel, IControllerEventUpdate
{
	[SerializeField]
	private List<OptionInputSettingPanel> inputSettingPanels;

	private SettingItem currentSettingItem;

	public override string ControllerName()
	{
		return "Option Control Panel";
	}

	public override void Initialize()
	{
		base.Initialize();
		foreach (OptionInputSettingPanel inputSettingPanel in inputSettingPanels)
		{
			inputSettingPanel.Initialize();
		}
		RewiredHelper.RegisterControllerChangeEvent(this);
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		RewiredHelper.FindMainController();
		base.VerticalIndex = ((RewiredHelper.Gamepad == null) ? 1 : 0);
		FocusCategory(base.VerticalIndex, force: true);
	}

	protected override void VerticalControl(int vertical)
	{
	}

	protected override void HorizontalControl(int horizontal)
	{
		currentSettingItem.Control(horizontal, 0);
	}

	protected override void OnConfirm()
	{
		currentSettingItem.OnConfirm();
	}

	protected override int GetNextIndex(int current, int delta)
	{
		return 0;
	}

	public void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
	}

	private void OnDestroy()
	{
		RewiredHelper.RemoveControllerChangeEvent(this);
	}
}
