using DG.Tweening;
using Library;
using SPR.Save;
using UnityEngine;

namespace SPR.UI.Trait;

public abstract class TraitInfoBoard : MonoBehaviour
{
	protected RectTransform rectTransform;

	private CanvasGroup canvasGroup;

	protected Sequence FadeSeq;

	protected RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	protected static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	public abstract void Initialize();

	public void SetActive(bool value, bool instant = false)
	{
		float num = (value ? 1f : 0f);
		if (instant)
		{
			CanvasGroup.alpha = num;
		}
		else
		{
			FadeSeq?.Kill();
			FadeSeq = DOTween.Sequence().Join(CanvasGroup.DOAlpha(num, 1f / 6f).SetEase(Ease.Linear));
		}
		OnSetActive(value, instant);
	}

	public abstract void SetInfo(TraitCardElement card, bool isRest, int point);

	protected virtual void OnSetActive(bool value, bool instant)
	{
	}
}
