using DG.Tweening;
using Library;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Field;

public class HeatLevel : MonoBehaviour
{
	[SerializeField]
	private Image backgroundImage;

	[SerializeField]
	private Image gaugeImage;

	[SerializeField]
	private Image shadowImage;

	[SerializeField]
	private Image lineImage;

	private bool active;

	private float targetAmount;

	private Sequence fullSequence;

	private Sequence cachedSequence;

	private static readonly Color ActiveColor = new Color(44f / 85f, 44f / 85f, 44f / 85f);

	private static readonly Color InactiveColor = new Color(21f / 85f, 21f / 85f, 21f / 85f);

	private static readonly Color OriginShadowColor = new Color(0f, 77f / 85f, 1f, 0f);

	public void SetActive(bool value)
	{
		if (active != value)
		{
			active = value;
			Color color = (value ? ActiveColor : InactiveColor);
			backgroundImage.color = color;
			ResetRect();
		}
	}

	public void UpdateHeatGauge(float amount, bool forced = false)
	{
		if (active || forced)
		{
			if (amount < 1f && gaugeImage.fillAmount >= 1f)
			{
				fullSequence?.Kill();
				fullSequence = GetUsingSequence();
				fullSequence.Play();
			}
			else if (amount >= 1f && (gaugeImage.fillAmount < 1f || targetAmount < 1f))
			{
				fullSequence?.Kill();
				ResetRect();
				fullSequence = GetFullSequence();
				fullSequence.Play();
				gaugeImage.fillAmount = amount;
			}
			targetAmount = amount;
		}
	}

	public void PlayLastEmberSequence()
	{
		fullSequence?.Kill();
		fullSequence = GetLastEmberSequence();
		fullSequence.Play();
	}

	public void ResetImages()
	{
		lineImage.color = OriginShadowColor;
		shadowImage.color = OriginShadowColor;
	}

	public Sequence GetOpenSequence()
	{
		backgroundImage.color = InactiveColor;
		RectTransform rectTransform = lineImage.rectTransform;
		rectTransform.localScale = Vector3.one;
		Color color = lineImage.color;
		color.g = 0.9f;
		color.a = 0f;
		lineImage.color = color;
		float fixedDeltaTime = Time.fixedDeltaTime;
		Sequence sequence = DOTween.Sequence();
		Sequence sequence2 = DOTween.Sequence();
		sequence2.Append(backgroundImage.DOColor(ActiveColor, 20f * fixedDeltaTime));
		Sequence sequence3 = DOTween.Sequence();
		sequence3.Append(rectTransform.DOScale(new Vector3(0.9f, 0.9f, 1f), 12f * fixedDeltaTime)).Append(rectTransform.DOScale(Vector3.one, 8f * fixedDeltaTime));
		Sequence sequence4 = DOTween.Sequence();
		sequence4.Append(lineImage.DOAlpha(1f, 7f * fixedDeltaTime)).AppendInterval(18f * fixedDeltaTime).Append(lineImage.DOAlpha(0f, 30f * fixedDeltaTime))
			.Join(lineImage.DOColor(new Color(color.r, 0f, color.b, 0f), 30f * fixedDeltaTime));
		sequence.Append(sequence2).Join(sequence3).Join(sequence4);
		return sequence;
	}

	private void ResetRect()
	{
		gaugeImage.rectTransform.anchoredPosition = Vector2.zero;
		gaugeImage.rectTransform.localScale = Vector3.one;
		gaugeImage.rectTransform.sizeDelta = Vector2.one;
		gaugeImage.SetAlpha(1f);
		lineImage.rectTransform.anchoredPosition = Vector2.zero;
		lineImage.rectTransform.localScale = Vector3.one;
		lineImage.rectTransform.sizeDelta = Vector2.one;
		lineImage.SetAlpha(0f);
		shadowImage.SetAlpha(0f);
		shadowImage.fillAmount = 0f;
	}

	private Sequence GetFullSequence()
	{
		shadowImage.color = new Color(1f, 1f, 1f, 0f);
		shadowImage.fillAmount = 1f;
		RectTransform rectTransform = lineImage.rectTransform;
		rectTransform.localScale = Vector3.one;
		lineImage.color = new Color(0f, 0.9f, 1f, 0f);
		float fixedDeltaTime = Time.fixedDeltaTime;
		Sequence sequence = DOTween.Sequence();
		Sequence sequence2 = DOTween.Sequence();
		sequence2.Append(shadowImage.DOAlpha(1f, 10f * fixedDeltaTime)).Append(DOTween.To(() => shadowImage.color.r, delegate(float r)
		{
			Color color = shadowImage.color;
			color.r = r;
			shadowImage.color = color;
		}, 0f, 20f * fixedDeltaTime)).Insert(0f, DOTween.To(() => shadowImage.color.g, delegate(float g)
		{
			Color color = shadowImage.color;
			color.g = g;
			shadowImage.color = color;
		}, 0.9f, 30f * fixedDeltaTime));
		Sequence sequence3 = DOTween.Sequence();
		sequence3.AppendInterval(9f * fixedDeltaTime).Append(rectTransform.DOScale(new Vector3(1.2f, 1.2f), fixedDeltaTime)).Append(rectTransform.DOScale(new Vector3(0.9f, 0.9f), 10f * fixedDeltaTime))
			.Append(rectTransform.DOScale(new Vector3(1f, 1f), 10f * fixedDeltaTime))
			.Insert(8f * fixedDeltaTime, lineImage.DOAlpha(1f, fixedDeltaTime));
		sequence.Append(sequence2).Join(sequence3);
		return sequence;
	}

	private Sequence GetFullDownSequence()
	{
		Color color = shadowImage.color;
		color.a = 1f;
		shadowImage.color = color;
		lineImage.rectTransform.localScale = Vector3.one;
		lineImage.color = new Color(0f, 0.9f, 1f, 1f);
		float fixedDeltaTime = Time.fixedDeltaTime;
		Sequence sequence = DOTween.Sequence();
		sequence.Append(shadowImage.DOAlpha(0f, 5f * fixedDeltaTime)).Join(lineImage.DOAlpha(0f, 5f * fixedDeltaTime));
		return sequence;
	}

	private Sequence GetUsingSequence()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		Sequence sequence = DOTween.Sequence();
		Color color = shadowImage.color;
		color.g = 0.9f;
		shadowImage.color = color;
		float duration = 5f * fixedDeltaTime;
		sequence.Append(gaugeImage.rectTransform.DOAnchorPos(new Vector2(-21f, 33f), duration)).Join(gaugeImage.transform.DOScale(new Vector3(1.25f, 1.25f, 1f), duration)).Join(gaugeImage.rectTransform.DOSizeDelta(new Vector2(7.8f, 0f), duration))
			.Join(shadowImage.DOAlpha(0f, duration))
			.Join(shadowImage.DOFillAmount(0f, duration))
			.Join(lineImage.rectTransform.DOAnchorPos(new Vector2(-21f, 33f), duration))
			.Join(lineImage.rectTransform.DOScale(new Vector3(1.25f, 1.25f, 1f), duration))
			.Join(lineImage.rectTransform.DOSizeDelta(new Vector2(7.8f, 0f), duration));
		duration = 15f * fixedDeltaTime;
		sequence.Append(gaugeImage.rectTransform.DOAnchorPos(new Vector2(-80f, 142f), duration)).Join(gaugeImage.transform.DOScale(new Vector3(2f, 2f, 1f), duration)).Join(gaugeImage.rectTransform.DOSizeDelta(new Vector2(0f, 0f), duration))
			.Join(gaugeImage.DOAlpha(0f, duration))
			.Join(lineImage.rectTransform.DOAnchorPos(new Vector2(-80f, 142f), duration))
			.Join(lineImage.rectTransform.DOScale(new Vector3(2f, 2f, 1f), duration))
			.Join(lineImage.rectTransform.DOSizeDelta(new Vector2(0f, 0f), duration))
			.Join(lineImage.DOAlpha(0f, duration));
		sequence.AppendCallback(delegate
		{
			gaugeImage.fillAmount = 0f;
			ResetRect();
		});
		return sequence;
	}

	private Sequence GetLastEmberSequence()
	{
		gaugeImage.fillAmount = 0f;
		shadowImage.fillAmount = 1f;
		shadowImage.color = new Color(0.6f, 0.15f, 0.15f, 0f);
		RectTransform rectTransform = lineImage.rectTransform;
		rectTransform.anchoredPosition = new Vector2(-25f, 36f);
		rectTransform.localScale = new Vector3(1.3f, 1.3f, 1f);
		rectTransform.sizeDelta = Vector2.zero;
		lineImage.color = new Color(1f, 0f, 0.15f, 0f);
		float duration = 20f * Time.fixedDeltaTime;
		Sequence sequence = DOTween.Sequence();
		sequence.Append(shadowImage.DOColor(new Color(0.25f, 0f, 0f, 0.64f), duration)).Join(rectTransform.DOAnchorPos(Vector2.zero, duration)).Join(rectTransform.DOScale(Vector2.one, duration))
			.Join(rectTransform.DOSizeDelta(Vector2.zero, duration))
			.Join(lineImage.DOAlpha(0.5f, duration));
		return sequence;
	}
}
