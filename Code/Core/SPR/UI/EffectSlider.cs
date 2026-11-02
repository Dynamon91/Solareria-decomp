using System;
using DG.Tweening;
using FMOD.Studio;
using Library;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class EffectSlider : MonoBehaviour
{
	[SerializeField]
	private RectTransform fillBackgroundRect;

	[SerializeField]
	protected Image fillFollowerImage;

	[SerializeField]
	protected Image fillImage;

	[SerializeField]
	private TextMeshProUGUI damageText;

	[Tooltip("true라면 수치가 0일 때, Slider를 비활성화 한다.")]
	[SerializeField]
	private bool disableWhenZero;

	[SerializeField]
	protected HealEffectBar healEffectBar;

	private EventInstance increaseInstance;

	private RectTransform barRect;

	public RectTransform BarRectTransform => barRect ?? (barRect = GetComponent<RectTransform>());

	public float FillAmount => fillImage.fillAmount;

	public RectTransform HealthRectTransform => fillBackgroundRect;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public virtual void Initialize()
	{
		fillFollowerImage.fillAmount = 1f;
		fillImage.fillAmount = 1f;
		damageText.text = "";
		damageText.alpha = 0f;
		damageText.gameObject.SetActive(value: false);
		healEffectBar.Initialize(this);
	}

	public void UpdateSlider(float current, float max)
	{
		DisableDamageText();
		float num = current / max;
		fillFollowerImage.DOKill();
		fillFollowerImage.DOFillAmount(num, (num < fillImage.fillAmount) ? 1f : 0f).SetEase(Ease.OutQuad);
		fillImage.fillAmount = num;
		if (disableWhenZero)
		{
			base.gameObject.SetActive(current > 0f);
		}
	}

	public void UpdateEnemySlider(float current, float max, int delta)
	{
		float num = current / max;
		if (damageText.IsNull() || delta > 0)
		{
			DisableDamageText();
			fillFollowerImage.DOKill();
			fillFollowerImage.DOFillAmount(num, (num < fillImage.fillAmount) ? 1f : 0.5f).SetEase(Ease.OutQuad);
			if (num > fillImage.fillAmount)
			{
				fillImage.DOKill();
				fillImage.DOFillAmount(num, 0.5f).SetEase(Ease.OutCubic);
			}
			else
			{
				fillImage.fillAmount = num;
			}
			if (disableWhenZero)
			{
				base.gameObject.SetActive(current > 0f);
			}
		}
		else
		{
			damageText.gameObject.SetActive(value: true);
			EnableDamageText(Mathf.Abs(delta));
			fillImage.fillAmount = num;
			fillFollowerImage.DOKill();
			fillFollowerImage.DOFillAmount(num, 1f).SetEase(Ease.OutQuad);
		}
	}

	private void EnableDamageText(int delta)
	{
		if (delta != 0)
		{
			damageText.DOKill();
			int num = 0;
			if (damageText.alpha > 0f && int.TryParse(damageText.text, out var result))
			{
				num = result;
			}
			damageText.alpha = 1f;
			damageText.text = (num + delta).ToString();
			damageText.DOFade(0f, 2.5f).SetEase(Ease.Linear);
		}
	}

	protected void DisableDamageText()
	{
		if (!damageText.IsNull())
		{
			damageText.gameObject.SetActive(value: false);
		}
	}

	public void PlayHealTween(bool loop, Type subscriberType)
	{
		healEffectBar.PlayHealTween(loop, subscriberType);
	}

	public void StopHealTween(Type subscriberType)
	{
		healEffectBar.StopHealTween(subscriberType);
	}

	protected void PlayIncreaseEvent()
	{
		if (!increaseInstance.isValid())
		{
			increaseInstance = SoundManager.PlayOneShot("event:/SFX/UI/System/SFX_UI_IncreaseMaxStat");
		}
	}

	protected void StopIncreaseEvent()
	{
		increaseInstance.stop(STOP_MODE.ALLOWFADEOUT);
		increaseInstance.release();
	}
}
