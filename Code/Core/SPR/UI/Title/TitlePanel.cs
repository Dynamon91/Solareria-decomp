using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Library;
using SPR.UI.Option;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Title;

public class TitlePanel : MonoBehaviour, IControllable, ILayoutRebuildParent
{
	[SerializeField]
	private TitleImage titleImage;

	[SerializeField]
	private CategoryList categoryList;

	[SerializeField]
	private List<LanguageWidthInfo> languageWidthInfos;

	[SerializeField]
	private RectTransform keyGuidesParent;

	[Header("데모 버전 세이브 호환 불가 경고")]
	[SerializeField]
	private bool warnDemoSave;

	private TitleCategory currentCategory;

	private int? rebuildLeftChildren;

	private static TitleManager TitleManager => Game.Instance.TitleManager;

	private static SaveSlotPanel SaveSlotPanel => TitleManager.SaveSlotPanel;

	private static TitleLanguagePanel LanguagePanel => TitleManager.LanguagePanel;

	private static UIManager UIManager => Game.Instance.UIManager;

	private static ConfirmPopup ConfirmPopup => Game.Instance.UIManager.FieldCanvas.ConfirmPopup;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public bool IsValid => !base.gameObject.IsNull();

	public void Initialize()
	{
		categoryList.Initialize();
	}

	public void FocusCategory(TitleCategory newCategory, bool force = false)
	{
		if (force || currentCategory != newCategory)
		{
			currentCategory = newCategory;
			categoryList.Focus((int)newCategory, sound: false);
			if (!force)
			{
				SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Move");
			}
		}
	}

	public void SelectChoice(TitleCategory selectedCategory)
	{
		switch (selectedCategory)
		{
		case TitleCategory.Start:
			if (warnDemoSave && Application.version.StartsWith("0."))
			{
				ConfirmPopup.SetModel(new ConfirmModel("WarnDemoSave", OpenSaveSlot));
				InputManager.StackControlTarget(ConfirmPopup).Forget();
			}
			else
			{
				OpenSaveSlot();
			}
			break;
		case TitleCategory.System:
			UIManager.OptionCanvas.SetTargetCategory(OptionCategory.Option);
			InputManager.StackControlTarget(UIManager.OptionCanvas).Forget();
			break;
		case TitleCategory.Language:
			InputManager.StackControlTarget(LanguagePanel).Forget();
			break;
		case TitleCategory.Quit:
			ConfirmPopup.SetModel(new ConfirmModel("SaveAndQuit", delegate
			{
				Game.QuitGameAsync().Forget();
			}));
			InputManager.StackControlTarget(ConfirmPopup).Forget();
			break;
		case TitleCategory.Discord:
			OpenDiscordPage();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		static void OpenSaveSlot()
		{
			InputManager.StackControlTarget(SaveSlotPanel).Forget();
			SoundManager.PlayOneShot("event:/SFX/UI/System/SFX_UI_System_GameStart");
		}
	}

	private void SetMarkerWidth()
	{
		float width = languageWidthInfos.First((LanguageWidthInfo i) => i.language == Game.Instance.SettingData.Language).width;
		categoryList.SetWidth(width);
	}

	public string ControllerName()
	{
		return "Title Panel";
	}

	public UniTask Activate()
	{
		UIManager.FieldCanvas.HideAllAsync(instant: true).Forget();
		base.gameObject.SetActive(value: true);
		titleImage.SetData();
		SetMarkerWidth();
		FocusCategory(TitleCategory.Start, force: true);
		UnityEngine.Cursor.visible = false;
		foreach (Transform item in keyGuidesParent.transform)
		{
			item.GetComponent<KeyGuideItem>().RegisterLayoutRebuildParent(this, setRebuildOnEnable: true);
		}
		return default(UniTask);
	}

	public void Control()
	{
		bool confirmDown = InputManager.confirmDown;
		int horizontalDownUI = InputManager.horizontalDownUI;
		int verticalDownUI = InputManager.verticalDownUI;
		if (confirmDown)
		{
			SelectChoice(currentCategory);
		}
		else if (verticalDownUI != 0)
		{
			TitleCategory newCategory = Util.AddToEnumWithClamp(currentCategory, -verticalDownUI);
			FocusCategory(newCategory);
		}
		else if (horizontalDownUI != 0)
		{
			TitleCategory newCategory2 = currentCategory;
			if (currentCategory <= TitleCategory.Quit && horizontalDownUI > 0)
			{
				newCategory2 = TitleCategory.Discord;
			}
			else if (currentCategory == TitleCategory.Discord && horizontalDownUI < 0)
			{
				newCategory2 = TitleCategory.Start;
			}
			FocusCategory(newCategory2);
		}
	}

	public UniTask Pause()
	{
		keyGuidesParent.gameObject.SetActive(value: false);
		return default(UniTask);
	}

	public UniTask Restart()
	{
		SetMarkerWidth();
		keyGuidesParent.gameObject.SetActive(value: true);
		return default(UniTask);
	}

	public UniTask Deactivate()
	{
		return default(UniTask);
	}

	private void OpenSteamPage()
	{
		uint appId = SteamUtils.GetAppID().m_AppId;
		AppId_t nAppID = new AppId_t(appId);
		if (IsSteamApiReady() && SteamUtils.IsOverlayEnabled())
		{
			Debug.Log("[OpenSteamStoreButton] Steam overlay is enabled. Opening overlay to Store...");
			SteamFriends.ActivateGameOverlayToStore(nAppID, EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
		}
		else
		{
			string url = $"steam://store/{appId}";
			Debug.LogWarning("[OpenSteamStoreButton] Steam overlay unavailable. Trying to open Steam client via URI...");
			Application.OpenURL(url);
		}
		static bool IsSteamApiReady()
		{
			try
			{
				return SteamAPI.IsSteamRunning();
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[OpenSteamStoreButton] Steam API not available. Reason: " + ex.Message);
				return false;
			}
		}
	}

	private void OpenDiscordPage()
	{
		Application.OpenURL("https://discord.com/invite/xkWM3mDPdQ");
	}

	public void TryRebuild()
	{
		int valueOrDefault = rebuildLeftChildren.GetValueOrDefault();
		if (!rebuildLeftChildren.HasValue)
		{
			valueOrDefault = keyGuidesParent.childCount;
			rebuildLeftChildren = valueOrDefault;
		}
		DebugLog.GeneralLog($"Rebuild : children = {rebuildLeftChildren}");
		rebuildLeftChildren--;
		if (rebuildLeftChildren == 0)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(keyGuidesParent);
			DebugLog.GeneralLog("Rebuild Complete");
			rebuildLeftChildren = null;
		}
	}
}
