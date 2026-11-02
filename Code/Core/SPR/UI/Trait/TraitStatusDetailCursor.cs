using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI.Trait;

public class TraitStatusDetailCursor : MonoBehaviour
{
	private RectTransform rectTransform;

	private CanvasGroup canvasGroup;

	private Sequence moveSeq;

	private Sequence fadeSeq;

	private bool isActive;

	private Vector3 targetPos;

	private RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	public void Initialize()
	{
		base.gameObject.SetActive(value: true);
		CanvasGroup.alpha = 0f;
	}

	public void Focus(TraitStatusDetailElement element, bool instant = false)
	{
		float duration = (instant ? 0f : (1f / 6f));
		if (element.IsNull())
		{
			isActive = false;
			fadeSeq?.Kill();
			fadeSeq = DOTween.Sequence().Join(CanvasGroup.DOFade(0f, duration).SetEase(MenuTraitPanel.GetEase(active: false)));
			return;
		}
		moveSeq?.Kill();
		moveSeq = DOTween.Sequence().Join(RectTransform.DOMove(element.RectTransform.position, duration).SetEase(Ease.InOutQuad));
		if (!isActive)
		{
			isActive = true;
			fadeSeq?.Kill();
			fadeSeq = DOTween.Sequence().Join(CanvasGroup.DOFade(1f, duration).SetEase(MenuTraitPanel.GetEase(active: true)));
		}
	}
}
