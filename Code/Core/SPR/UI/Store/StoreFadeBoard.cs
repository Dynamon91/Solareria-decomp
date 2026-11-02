using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI.Store;

public class StoreFadeBoard : MonoBehaviour
{
	[SerializeField]
	private float fadeDuration;

	[SerializeField]
	private float sustainDuration;

	[SerializeField]
	private float displacementY;

	private RectTransform rect;

	private CanvasGroup canvasGroup;

	private LocaleText localeText;

	private Sequence fadeSeq;

	private RectTransform Rect => rect ?? (rect = GetComponent<RectTransform>());

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private LocaleText LocaleText => localeText ?? (localeText = GetComponentInChildren<LocaleText>());

	public void Initialize(string messageLocaleKey)
	{
		CanvasGroup.alpha = 0f;
		Rect.anchoredPosition = new Vector2(Rect.anchoredPosition.x, displacementY);
		LocaleText.SetData(messageLocaleKey);
		base.gameObject.SetActive(value: true);
	}

	public async UniTask FadeInOutAsync(TweenCallback onSustain = null)
	{
		fadeSeq?.Kill();
		fadeSeq = DOTween.Sequence();
		Rect.anchoredPosition = new Vector2(Rect.anchoredPosition.x, displacementY);
		await fadeSeq.Append(CanvasGroup.DOAlpha(1f, fadeDuration).SetEase(Ease.OutQuad)).Join(Rect.DOAnchorPosY(0f, fadeDuration).SetEase(Ease.OutQuad)).AppendInterval(sustainDuration)
			.AppendCallback(onSustain)
			.Append(CanvasGroup.DOAlpha(0f, fadeDuration).SetEase(Ease.InQuad))
			.Join(Rect.DOAnchorPosY(displacementY, fadeDuration).SetEase(Ease.InQuad))
			.WaitAsync();
	}
}
