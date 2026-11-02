using System;
using Cysharp.Threading.Tasks;
using SPR.UI.Menu;
using SPR.UI.Option;
using UnityEngine;

namespace SPR.UI.System;

public class MenuSystemPanel : MenuPanel
{
	[SerializeField]
	private CategoryList categoryList;

	[SerializeField]
	private GameObject devModeCategory;

	private MenuSystemCategory currentCategory;

	public override MenuCategory Category => MenuCategory.System;

	private static OptionCanvas OptionCanvas => Game.Instance.UIManager.OptionCanvas;

	private static ConfirmPopup ConfirmPopup => Game.Instance.UIManager.FieldCanvas.ConfirmPopup;

	private bool EnableDevModeCategory => false;

	public override void Initialize()
	{
		categoryList.Initialize();
	}

	protected override void OnActivate()
	{
		FocusCategory(currentCategory, force: true);
	}

	protected override void OnControl()
	{
		bool confirmDown = MenuPanel.InputManager.confirmDown;
		int verticalDownUI = MenuPanel.InputManager.verticalDownUI;
		if (verticalDownUI != 0)
		{
			MenuSystemCategory menuSystemCategory = Util.AddToEnumWithClamp(currentCategory, -verticalDownUI);
			if (!EnableDevModeCategory && menuSystemCategory == MenuSystemCategory.DevMode)
			{
				menuSystemCategory = MenuSystemCategory.Quit;
			}
			FocusCategory(menuSystemCategory);
		}
		if (!confirmDown)
		{
			return;
		}
		switch (currentCategory)
		{
		case MenuSystemCategory.Option:
			OptionCanvas.SetTargetCategory(OptionCategory.Option);
			MenuPanel.InputManager.StackControlTarget(OptionCanvas).Forget();
			break;
		case MenuSystemCategory.Tutorial:
			OptionCanvas.SetTargetCategory(OptionCategory.Tutorial);
			MenuPanel.InputManager.StackControlTarget(OptionCanvas).Forget();
			break;
		case MenuSystemCategory.BackToTitle:
			Game.Instance.Save();
			BackToTitleAsync().Forget();
			break;
		case MenuSystemCategory.Quit:
			ConfirmPopup.SetModel(new ConfirmModel("SaveAndQuit", delegate
			{
				Game.Instance.Save();
				Game.QuitGameAsync().Forget();
			}));
			MenuPanel.InputManager.StackControlTarget(ConfirmPopup).Forget();
			break;
		case MenuSystemCategory.DevMode:
			ToggleDevMode();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	protected override void OnDeactivate()
	{
	}

	private void FocusCategory(MenuSystemCategory newCategory, bool force = false)
	{
		if (force || currentCategory != newCategory)
		{
			currentCategory = newCategory;
			categoryList.Focus((int)newCategory, !force);
		}
	}

	private async UniTask BackToTitleAsync()
	{
		await DeactivateMenuAsync();
		if (MenuPanel.InputManager.ContainsInControlStack(MenuPanel.RestCanvas))
		{
			await MenuPanel.InputManager.RemoveControlTarget(MenuPanel.RestCanvas);
		}
		await Game.Instance.LoadTitleAsync();
		await MenuPanel.InputManager.RemoveControlTarget(Game.Instance.Player);
	}

	private void Awake()
	{
		if (!EnableDevModeCategory)
		{
			devModeCategory.SetActive(value: false);
		}
	}

	private void ToggleDevMode()
	{
	}
}
