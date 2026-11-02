using System.Collections.Generic;
using DG.Tweening;
using Library;
using SPR.L10N;
using SPR.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.Achievement;

public class AchievementToastUI : MonoBehaviour
{
	[Header("UI Components")]
	[SerializeField]
	private LocaleText titleText;

	[SerializeField]
	private Image iconImage;

	[Header("Animation Settings")]
	[SerializeField]
	private float slideDuration = 0.5f;

	[SerializeField]
	private float displayDuration = 2f;

	private RectTransform rectTransform;

	private Queue<string> achievementQueue = new Queue<string>();

	private bool isAnimating;

	private readonly Vector2 hiddenPos = new Vector2(0f, -120f);

	private readonly Vector2 visiblePos = new Vector2(0f, 0f);

	private RectTransform Rect => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	private void Awake()
	{
		Rect.anchoredPosition = hiddenPos;
	}

	private void OnDestroy()
	{
		Rect.DOKill();
	}

	public void Show(string achievementId)
	{
		achievementQueue.Enqueue(achievementId);
		if (!isAnimating)
		{
			ProcessQueue();
		}
	}

	private void ProcessQueue()
	{
		if (achievementQueue.Count == 0)
		{
			isAnimating = false;
			return;
		}
		isAnimating = true;
		string id = achievementQueue.Dequeue();
		UpdateUI(id);
		PlayAnimationSequence();
	}

	private void UpdateUI(string id)
	{
		if (!titleText.IsNull())
		{
			titleText.SetData(LocaleKey.AchievementName(id));
		}
	}

	private void PlayAnimationSequence()
	{
		Rect.DOKill();
		Rect.anchoredPosition = hiddenPos;
		DOTween.Sequence().Append(Rect.DOAnchorPos(visiblePos, slideDuration).SetEase(Ease.OutBack)).AppendInterval(displayDuration)
			.Append(Rect.DOAnchorPos(hiddenPos, slideDuration).SetEase(Ease.InCubic))
			.OnComplete(ProcessQueue);
	}
}
