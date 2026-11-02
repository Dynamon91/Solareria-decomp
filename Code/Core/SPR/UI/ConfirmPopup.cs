using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI;

public class ConfirmPopup : MonoBehaviour, IControllable
{
	[SerializeField]
	private RectTransform indicator;

	[Header("Text")]
	[SerializeField]
	private LocaleText titleText;

	[SerializeField]
	private LocaleText messageText;

	[SerializeField]
	private LocaleText confirmChoiceText;

	[SerializeField]
	private LocaleText cancelChoiceText;

	private const float FadeDuration = 0.16f;

	private const float IndicatorMoveDuration = 0.2f;

	private Action onConfirm;

	private Action onCancel;

	private CanvasGroup canvasGroup;

	private RectTransform confirmRect;

	private RectTransform cancelRect;

	private int currentIndex;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private RectTransform YesRect => confirmRect ?? (confirmRect = confirmChoiceText.GetComponent<RectTransform>());

	private RectTransform NoRect => cancelRect ?? (cancelRect = cancelChoiceText.GetComponent<RectTransform>());

	private static InputManager InputManager => Game.Instance.InputManager;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public bool IsValid => !base.gameObject.IsNull();

	public void SetModel(ConfirmModel model)
	{
		titleText.SetData(model.TitleKey);
		messageText.SetData(model.MessageKey);
		confirmChoiceText.SetData(model.ConfirmChoiceKey);
		cancelChoiceText.SetData(model.CancelChoiceKey);
		onConfirm = model.OnConfirm;
		onCancel = model.OnCancel;
	}

	public void SetModel(FormatConfirmModel model)
	{
		titleText.SetData(model.TitleKey);
		messageText.SetDataFormat(model.MessageKey, model.FormattedStrings);
		confirmChoiceText.SetData(model.ConfirmChoiceKey);
		cancelChoiceText.SetData(model.CancelChoiceKey);
		onConfirm = model.OnConfirm;
		onCancel = model.OnCancel;
	}

	private void ChangeTargetTo(int index, bool instant = false)
	{
		if (currentIndex != index)
		{
			currentIndex = index;
			float x = ((currentIndex == 0) ? YesRect : NoRect).anchoredPosition.x;
			indicator.DOKill();
			if (instant)
			{
				indicator.anchoredPosition = new Vector2(x, indicator.anchoredPosition.y);
			}
			else
			{
				indicator.DOAnchorPosX(x, 0.2f);
			}
		}
	}

	private void Select()
	{
		if (currentIndex == 0)
		{
			onConfirm?.Invoke();
		}
		else
		{
			onCancel?.Invoke();
		}
		InputManager.RemoveControlTarget(this).Forget();
	}

	public string ControllerName()
	{
		return "Confirm Popup";
	}

	public async UniTask Activate()
	{
		CanvasGroup.alpha = 0f;
		base.gameObject.SetActive(value: true);
		ChangeTargetTo(0, instant: true);
		SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem_Show");
		await CanvasGroup.DOFade(1f, 0.16f).WaitAsync();
	}

	public void Control()
	{
		int horizontalDownUI = InputManager.horizontalDownUI;
		bool confirmDown = InputManager.confirmDown;
		bool flag = InputManager.menuDown || InputManager.cancelDown;
		if (horizontalDownUI != 0)
		{
			int index = Mathf.Clamp(currentIndex + horizontalDownUI, 0, 1);
			ChangeTargetTo(index);
			SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
		}
		else if (confirmDown)
		{
			Select();
			SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Interaction");
		}
		else if (flag)
		{
			onCancel?.Invoke();
			InputManager.RemoveControlTarget(this).Forget();
			SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Interaction");
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

	public async UniTask Deactivate()
	{
		await CanvasGroup.DOFade(0f, 0.16f).WaitAsync();
		base.gameObject.SetActive(value: false);
	}
}
