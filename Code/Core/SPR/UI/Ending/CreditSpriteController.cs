using System;
using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Ending;

[Obsolete]
public class CreditSpriteController : MonoBehaviour, IControllable
{
	[SerializeField]
	private Image illustImage;

	[SerializeField]
	private TitleSpriteAsset spriteAsset;

	[SerializeField]
	private CanvasGroup keyGuideCanvasGroup;

	private static InputManager InputManager => Game.Instance.InputManager;

	public bool IsValid => !base.gameObject.IsNull();

	public void SetActive(bool value, bool instant)
	{
		illustImage.SetAlpha(1f);
		illustImage.gameObject.SetActive(value);
		illustImage.enabled = value;
		if (value)
		{
			Sprite sprite = ((Game.Instance.SaveData.TalkData.GetTalkCount("EndingCutscene_Thank_Real") > 0) ? spriteAsset.trueEndingSprite : spriteAsset.normalEndingSprite);
			illustImage.sprite = sprite;
		}
		if (instant)
		{
			keyGuideCanvasGroup.alpha = (value ? 1f : 0f);
		}
	}

	public string ControllerName()
	{
		return "Credit Sprite Controller";
	}

	public async UniTask Activate()
	{
		SetActive(value: true, instant: false);
		await Game.Instance.UIManager.FadeCanvas.FadeIn(1f);
		await UniTask.WaitForSeconds(5f);
		await keyGuideCanvasGroup.DOAlpha(1f, 0.5f).WaitAsync();
	}

	public async void Control()
	{
		bool confirmDown = InputManager.confirmDown;
		bool flag = InputManager.menuDown || InputManager.cancelDown;
		if (confirmDown || flag)
		{
			await InputManager.RemoveControlTarget(this);
			await InputManager.RemoveControlTarget(Game.Instance.CreditManager);
			await Game.Instance.UIManager.FadeCanvas.FadeOut(0.4f);
			await UniTask.WaitForSeconds(4f);
			await Game.Instance.LoadTitleAsync();
		}
	}

	public UniTask Pause()
	{
		return default(UniTask);
	}

	public UniTask Restart()
	{
		return default(UniTask);
	}

	public UniTask Deactivate()
	{
		return default(UniTask);
	}
}
