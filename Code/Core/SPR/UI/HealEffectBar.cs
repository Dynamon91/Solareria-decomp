using System;
using System.Collections.Generic;
using DG.Tweening;
using Library;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class HealEffectBar : MonoBehaviour
{
	[SerializeField]
	private Image healImage;

	private EffectSlider targetEffectSlider;

	private List<Type> healSubscribers;

	public void Initialize(EffectSlider effectSlider)
	{
		targetEffectSlider = effectSlider;
		healImage.gameObject.SetActive(value: false);
	}

	public void PlayHealTween(bool loop, Type subscriberType)
	{
		if (loop)
		{
			if (healSubscribers == null)
			{
				healSubscribers = new List<Type>();
			}
			healSubscribers.Add(subscriberType);
		}
		PlayHealTween(loop);
	}

	public void StopHealTween(Type subscriberType)
	{
		if (!healSubscribers.IsNullOrEmpty())
		{
			healSubscribers.Remove(subscriberType);
			if (healSubscribers.Count <= 0)
			{
				StopHealTween();
			}
		}
	}

	public void PlayHealTween(bool loop)
	{
		healImage.gameObject.SetActive(value: true);
		healImage.DOKill();
		healImage.SetAlpha(0f);
		Tween t = healImage.DOAlpha(1f, 0.25f);
		t.SetLoops((!loop) ? 1 : (-1), LoopType.Yoyo);
		t.Play();
		if (!loop)
		{
			t.OnComplete(delegate
			{
				healImage.gameObject.SetActive(value: false);
			});
		}
	}

	public void StopHealTween()
	{
		healImage.DOKill();
		healImage.gameObject.SetActive(value: false);
	}

	private void LateUpdate()
	{
		if (healImage.gameObject.activeSelf)
		{
			UpdateHealBar();
		}
	}

	private void UpdateHealBar()
	{
		float x = Mathf.Lerp(0f, targetEffectSlider.HealthRectTransform.rect.width, targetEffectSlider.FillAmount);
		Vector2 anchoredPosition = healImage.rectTransform.anchoredPosition;
		anchoredPosition.x = x;
		healImage.rectTransform.anchoredPosition = anchoredPosition;
	}
}
