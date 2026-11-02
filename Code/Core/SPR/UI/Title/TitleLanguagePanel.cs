using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using TMPro;
using UnityEngine;

namespace SPR.UI.Title;

public class TitleLanguagePanel : MonoBehaviour, IControllable
{
	[SerializeField]
	private LocaleText titleText;

	[SerializeField]
	private List<TextMeshProUGUI> options;

	[SerializeField]
	private OptionSelectMarker selectMarker;

	private int targetIndex;

	private Language currentLanguage;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public bool IsValid => !base.gameObject.IsNull();

	private int GetNextTargetIndex(int vertical)
	{
		int num = targetIndex - vertical;
		if (num < 0)
		{
			num = 0;
		}
		else if (num >= options.Count)
		{
			num = options.Count - 1;
		}
		return num;
	}

	private void ChangeTargetTo(int index, bool immediately = false)
	{
		if (targetIndex != index || immediately)
		{
			targetIndex = index;
			selectMarker.MoveToAsync(options[targetIndex].rectTransform, immediately).Forget();
			if (!immediately)
			{
				SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
			}
		}
	}

	private void ChangeLanguage()
	{
		if (currentLanguage != (Language)(targetIndex + 1))
		{
			currentLanguage = (Language)(targetIndex + 1);
			Game.Instance.SettingData.Language = currentLanguage;
			TitlePanel titlePanel = Game.Instance.TitleManager.TitlePanel;
			titlePanel.gameObject.SetActive(value: false);
			titlePanel.gameObject.SetActive(value: true);
		}
		InputManager.RemoveControlTarget(this).Forget();
	}

	public string ControllerName()
	{
		return "Title Language Panel";
	}

	public UniTask Activate()
	{
		base.gameObject.SetActive(value: true);
		titleText.SetData("Title_LanguagePanel_Title");
		currentLanguage = Game.CurrentLanguage;
		ChangeTargetTo((int)(currentLanguage - 1), immediately: true);
		SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Confirm");
		return UniTask.CompletedTask;
	}

	public void Control()
	{
		bool num = InputManager.menuDown || InputManager.cancelDown;
		bool confirmDown = InputManager.confirmDown;
		int verticalDownUI = InputManager.verticalDownUI;
		if (confirmDown)
		{
			ChangeLanguage();
		}
		if (verticalDownUI != 0)
		{
			int nextTargetIndex = GetNextTargetIndex(verticalDownUI);
			ChangeTargetTo(nextTargetIndex);
		}
		if (num)
		{
			InputManager.RemoveControlTarget(this).Forget();
		}
	}

	public UniTask Pause()
	{
		return UniTask.CompletedTask;
	}

	public UniTask Restart()
	{
		return UniTask.CompletedTask;
	}

	public UniTask Deactivate()
	{
		SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Confirm");
		base.gameObject.SetActive(value: false);
		return UniTask.CompletedTask;
	}
}
