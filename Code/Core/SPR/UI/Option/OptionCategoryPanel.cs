using UnityEngine;

namespace SPR.UI.Option;

[RequireComponent(typeof(SettingCategoryList))]
public abstract class OptionCategoryPanel : OptionPanel
{
	[SerializeField]
	private SettingCategoryList categoryList;

	protected int VerticalIndex { get; set; }

	protected virtual int DefaultIndex => 0;

	public override void Initialize()
	{
		categoryList.Initialize();
		base.Initialize();
	}

	protected override void OnActivate()
	{
		VerticalIndex = DefaultIndex;
		FocusCategory(VerticalIndex, force: true);
	}

	protected override void OnControl()
	{
		int verticalDownUI = OptionPanel.InputManager.verticalDownUI;
		int horizontalDownUI = OptionPanel.InputManager.horizontalDownUI;
		if (verticalDownUI != 0)
		{
			int nextIndex = GetNextIndex(VerticalIndex, -verticalDownUI);
			if (VerticalIndex == nextIndex)
			{
				return;
			}
			VerticalIndex = nextIndex;
			FocusCategory(VerticalIndex);
			VerticalControl(verticalDownUI);
		}
		if (horizontalDownUI != 0)
		{
			HorizontalControl(horizontalDownUI);
		}
		if (OptionPanel.InputManager.confirmDown)
		{
			OnConfirm();
		}
	}

	protected override void OnDeactivate()
	{
		Game.Instance.Save();
	}

	protected abstract void VerticalControl(int vertical);

	protected abstract void HorizontalControl(int horizontal);

	protected abstract void OnConfirm();

	protected abstract int GetNextIndex(int current, int delta);

	protected void FocusCategory(int targetIndex, bool force = false)
	{
		categoryList.Focus(targetIndex, !force);
	}
}
