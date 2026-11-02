using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using SPR.L10N;
using SPR.Save;
using SPR.UI.Menu;
using SPR.UI.Tutorial;
using UnityEngine;

namespace SPR.UI.Rest;

public class RestCanvas : AutoScaledCanvas, IControllable
{
	[SerializeField]
	private LocaleText sectorNameText;

	[SerializeField]
	private LocaleText altarNameText;

	[SerializeField]
	private CategoryList categoryList;

	private RestCategory currentCategory;

	private CanvasGroup canvasGroup;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private static InputManager InputManager => Game.Instance.InputManager;

	private static FieldController Field => Game.Instance.FieldController;

	private static UIManager UIManager => Game.Instance.UIManager;

	private static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	private static TutorialCanvas TutorialCanvas => UIManager.TutorialCanvas;

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private static FieldID CurrentFieldID => Game.Instance.CurrentFieldID;

	public bool IsValid => true;

	public void Initialize()
	{
		categoryList.Initialize();
		base.gameObject.SetActive(value: false);
	}

	public void FocusCategory(RestCategory newCategory, bool force = false)
	{
		if (force || currentCategory != newCategory)
		{
			currentCategory = newCategory;
			categoryList.Focus((int)newCategory);
			if (!force)
			{
				SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Move");
			}
		}
	}

	public void SelectChoice(RestCategory selectedCategory)
	{
		MenuCanvas menuCanvas = UIManager.MenuCanvas;
		switch (selectedCategory)
		{
		case RestCategory.FastTravel:
			menuCanvas.SetDefaultOpenCategory(MenuCategory.Map);
			menuCanvas.CategoryList.CanvasGroup.alpha = 0f;
			InputManager.StackControlTarget(menuCanvas).GetAwaiter().OnCompleted(delegate
			{
				TryShowTutorial(ActionName.FastTravel);
			});
			break;
		case RestCategory.Equipment:
			menuCanvas.SetDefaultOpenCategory(MenuCategory.Equipment);
			menuCanvas.CategoryList.CanvasGroup.alpha = 0f;
			InputManager.StackControlTarget(menuCanvas).Forget();
			break;
		case RestCategory.Trait:
			menuCanvas.SetDefaultOpenCategory(MenuCategory.Trait);
			menuCanvas.CategoryList.CanvasGroup.alpha = 0f;
			InputManager.StackControlTarget(menuCanvas).Forget();
			break;
		case RestCategory.Depart:
			InputManager.RemoveControlTarget(this).Forget();
			Field.ExitSavePlayerAsync().Forget();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public string ControllerName()
	{
		return "Rest Canvas";
	}

	public async UniTask Activate()
	{
		base.gameObject.SetActive(value: true);
		CanvasGroup.DOKill();
		CanvasGroup.alpha = 1f;
		PlayerStat.RecoverStat();
		SectorName sectorNameForPotteryRoad = FieldID.GetSectorNameForPotteryRoad(CurrentFieldID);
		sectorNameText.SetData(LocaleKey.SectorName(sectorNameForPotteryRoad));
		altarNameText.SetRawText(Locale.GetAltarLocaleText(CurrentFieldID));
		FocusCategory(RestCategory.FastTravel, force: true);
		SoundManager.FadeOnBonfire(isActive: true);
		await UIManager.FieldCanvas.HideAllAsync(instant: true);
		if (!TryShowTutorial(ActionName.Bonfire))
		{
			TutorialCanvas.SetNotStopTime();
			TryShowTutorial();
		}
	}

	public void Control()
	{
		bool num = InputManager.menuDown || InputManager.cancelDown;
		bool confirmDown = InputManager.confirmDown;
		int verticalDownUI = InputManager.verticalDownUI;
		if (confirmDown)
		{
			SelectChoice(currentCategory);
		}
		if (verticalDownUI != 0)
		{
			RestCategory newCategory = Util.AddToEnumWithClamp(currentCategory, -verticalDownUI);
			FocusCategory(newCategory);
		}
		if (num)
		{
			InputManager.RemoveControlTarget(this).Forget();
			Field.ExitSavePlayerAsync().Forget();
		}
	}

	public UniTask Pause()
	{
		CanvasGroup.DOKill();
		CanvasGroup.DOFade(0f, 0.4f);
		return default(UniTask);
	}

	public UniTask Restart()
	{
		CanvasGroup.DOKill();
		CanvasGroup.DOFade(1f, 0.4f);
		TryShowTutorial();
		return default(UniTask);
	}

	public async UniTask Deactivate()
	{
		SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem_Hide");
		base.gameObject.SetActive(value: false);
		UIManager.FieldCanvas.ShowAllAsync(instant: false).Forget();
		UIManager.MenuCanvas.CategoryList.CanvasGroup.alpha = 1f;
		Game.Instance.Save();
		SoundManager.FadeOnBonfire(isActive: false);
		SoundManager.FadeInSFXEmitters(0.5f);
		await UniTask.Yield();
	}

	private void TryShowTutorial()
	{
		if (!CurrentFieldID.Equals(SectorName.Library, 3) || !TryShowTutorial(ActionName.Teleport))
		{
			Game.Instance.RuntimeData.ShowStatusEffectTutorial().Forget();
			if (WanderingData.ShouldShowTutorialMessage(ActionName.Part))
			{
				UIManager.FieldCanvas.TutorialMessageBoard.AddMessage(ActionName.Part);
			}
		}
	}

	private static bool TryShowTutorial(ActionName actionName)
	{
		if (WanderingData.ShouldShowTutorial(actionName))
		{
			TutorialCanvas.SetData(actionName);
			InputManager.StackControlTarget(TutorialCanvas).Forget();
			DebugLog.GeneralLog($"Rest Tutorial : {actionName}");
			return true;
		}
		return false;
	}
}
