using System.Collections.Generic;
using DG.Tweening;
using Dreamteck;
using SPR.Save;
using UnityEngine;

namespace SPR.UI;

public abstract class CarouselBoard<T> : MonoBehaviour where T : CarouselElement
{
	[SerializeField]
	private float centerDistance;

	[SerializeField]
	private int centerHeight;

	[SerializeField]
	private float shrinkScale;

	[SerializeField]
	private float fadeAmount;

	[SerializeField]
	private RectTransform leftArrow;

	[SerializeField]
	private RectTransform rightArrow;

	private List<CarouselElement> elements;

	private Sequence arrowMoveSequence;

	private const float FocusDuration = 0.5f;

	private static ObjectPoolManager PoolManager => Game.Instance.ObjectPoolManager;

	public int TotalElementCount => elements?.Count ?? 0;

	public void SetData(List<CardData> cards)
	{
		if (elements == null)
		{
			elements = new List<CarouselElement>();
		}
		elements.ForEach(delegate(CarouselElement board)
		{
			board.ReturnToPool();
		});
		elements.Clear();
		base.transform.DestroyChildren();
		foreach (CardData card in cards)
		{
			T val = PoolManager.GetObject<T>("UI/TraitCardBoard.prefab", base.transform);
			val.SetHeight(centerHeight);
			val.SetData(card.ID);
			elements.Add(val);
		}
	}

	public void Focus(int index, bool? shouldIncrease = null, bool instant = false)
	{
		if (index < 0 || index >= elements.Count)
		{
			return;
		}
		for (int i = 0; i < elements.Count; i++)
		{
			CarouselElement carouselElement = elements[i];
			float num = ((i == index) ? ((float)centerHeight) : ((float)centerHeight * shrinkScale));
			float num2 = ((i == index) ? 1f : fadeAmount);
			Vector2 vector = new Vector2(centerDistance * (float)(i - index), 0f);
			carouselElement.DOKillAll();
			if (instant)
			{
				carouselElement.SetAnchorPosition(vector);
				carouselElement.SetHeight(num);
				carouselElement.SetAlpha(num2);
				continue;
			}
			if (shouldIncrease.HasValue)
			{
				TweenArrow(shouldIncrease.Value);
			}
			carouselElement.DOAnchorPos(vector, 0.5f).SetEase(Ease.OutQuart);
			carouselElement.DOHeight(num, 0.5f).SetEase(Ease.OutQuart);
			carouselElement.DOAlpha(num2, 0.5f).SetEase(Ease.OutQuart);
		}
	}

	public T GetElementAt(int index)
	{
		if (index < 0 || index >= elements.Count)
		{
			return null;
		}
		return (T)elements[index];
	}

	private void TweenArrow(bool isIncrease)
	{
		arrowMoveSequence.Kill(complete: true);
		RectTransform rectTransform = (isIncrease ? rightArrow : leftArrow);
		int num = (isIncrease ? 5 : (-5));
		float x = rectTransform.anchoredPosition.x;
		arrowMoveSequence = DOTween.Sequence();
		arrowMoveSequence.Append(rectTransform.DOAnchorPosX(x + (float)num, 1f / 6f)).Append(rectTransform.DOAnchorPosX(x, 1f / 12f));
	}
}
