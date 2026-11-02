using DG.Tweening;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR.UI;

public abstract class CarouselElement : MonoBehaviour, IPoolableObject
{
	[Header("Carousel Element")]
	[SerializeField]
	private float widthHeightRatio = 0.66f;

	private RectTransform rectTransform;

	private CanvasGroup canvasGroup;

	public RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	public CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	public abstract string PrefabKey { get; }

	public abstract GameObject GameObject { get; }

	public abstract void SetData(int id);

	public void SetAnchorPosition(Vector2 targetPos)
	{
		RectTransform.anchoredPosition = targetPos;
	}

	public void SetHeight(float targetHeight)
	{
		float x = widthHeightRatio * targetHeight;
		RectTransform.sizeDelta = new Vector2(x, targetHeight);
	}

	public void SetAlpha(float alpha)
	{
		CanvasGroup.alpha = alpha;
	}

	public void DOKillAll()
	{
		this.DOKill();
		RectTransform.DOKill();
	}

	public Tween DOAnchorPos(Vector2 targetPos, float duration)
	{
		return RectTransform.DOAnchorPos(targetPos, duration);
	}

	public Tween DOHeight(float targetHeight, float duration)
	{
		return DOTween.To(() => RectTransform.sizeDelta.y, SetHeight, targetHeight, duration);
	}

	public Tween DOAlpha(float targetAlpha, float duration)
	{
		return CanvasGroup.DOFade(targetAlpha, duration);
	}

	public abstract void InitializeBeforeUse();

	public abstract void ReturnToPool();
}
