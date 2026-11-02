using Coffee.UIExtensions;
using DG.Tweening;
using Library;
using StaticData;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class HpSlider : EffectSlider
{
	[SerializeField]
	private Image damageFillImage;

	[SerializeField]
	private RectTransform healthRect;

	[SerializeField]
	private RectTransform shieldOutline;

	[SerializeField]
	private float sliderDuration = 1f / 6f;

	[Header("Dark Bar")]
	[SerializeField]
	private RectTransform darkRect;

	[SerializeField]
	private Image darkTexture;

	[SerializeField]
	private Image darkLine;

	[Header("Max Increase")]
	[SerializeField]
	private Image increaseImage;

	[SerializeField]
	private UIParticle increaseParticle;

	private Sequence damageSequence;

	private Sequence maxSequence;

	private float cachedMaxHealth;

	private Sequence darkSequence;

	private Sequence darkActivateSequence;

	private Sequence darkTextureSequence;

	private float targetNormalizedDarkValue;

	private const float BaseDarkBarWidth = 19f;

	private static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	private static Health Health => PlayerStat.GetStat<Health>();

	private static float ResidualHeat => PlayerStat.ResidualHeat.Value;

	public override void Initialize()
	{
		damageFillImage.fillAmount = 0f;
		damageFillImage.SetAlpha(0f);
		shieldOutline.gameObject.SetActive(value: false);
		Vector2 sizeDelta = shieldOutline.sizeDelta;
		sizeDelta.x = 0f;
		shieldOutline.sizeDelta = sizeDelta;
		healEffectBar.Initialize(this);
		DOAfterheat(Health.MaxHealth, Health.MaxHealthWithDark, 0f);
		targetNormalizedDarkValue = 0f;
		darkSequence?.Kill();
		darkActivateSequence?.Kill();
		darkTextureSequence?.Kill();
		SetDarkBar(0f);
		Color color = darkLine.color;
		color.a = 0f;
		darkLine.color = color;
		color = Color.gray;
		color.a = 0f;
		darkTexture.color = color;
	}

	public void PlayMaxChangedEffect(Vector2 baseSize)
	{
		int maxHealth = Health.MaxHealth;
		if ((float)maxHealth > cachedMaxHealth && cachedMaxHealth > 0f)
		{
			PlayMaxIncrease(baseSize, Health.CurrentHealth, Health.MaxHealthWithDark, Health.MaxHealth);
		}
		UpdateCachedHealth(maxHealth);
	}

	public void UpdateBarSize(Vector2 baseSize, bool instant)
	{
		int maxHealth = Health.MaxHealth;
		if (!instant)
		{
			if ((float)maxHealth > cachedMaxHealth)
			{
				return;
			}
		}
		else
		{
			UpdateCachedHealth(maxHealth);
		}
		Difficulty difficulty = Game.Instance.SaveData.Difficulty;
		float num = baseSize.x / (40f * Game.DataManager.GetCurrentDifficultyStat(StatType.Warmth.ToString(), difficulty));
		RectTransform barRectTransform = base.BarRectTransform;
		barRectTransform.sizeDelta = new Vector2(num * (float)Health.MaxHealth, barRectTransform.rect.height);
		UpdateSlider(Health.CurrentHealth, Health.MaxHealthWithDark, Health.MaxHealth, instant: true);
	}

	public void UpdateSlider(int current, int maxWithDark, int realMax, bool instant = false)
	{
		UpdateDarkBarSlider(maxWithDark, realMax);
		float num = ((current * maxWithDark == 0) ? 0f : ((float)current / (float)maxWithDark));
		float num2 = ((maxWithDark == 0) ? 0f : (((float)current + ResidualHeat) / (float)maxWithDark));
		if (instant)
		{
			ResetTween();
			fillImage.fillAmount = num;
			damageFillImage.fillAmount = num;
			DOAfterheat(num, num2, 0f);
		}
		else if (num < fillImage.fillAmount)
		{
			UpdateSliderOnDamaged(num, num2);
		}
		else if (num > fillImage.fillAmount)
		{
			ResetTween();
			fillImage.DOFillAmount(num, sliderDuration).SetEase(Ease.OutCubic);
			damageFillImage.DOFillAmount(num, sliderDuration).SetEase(Ease.OutCubic);
			DOAfterheat(num, num2, sliderDuration);
		}
		else if (!Mathf.Approximately(num2, num))
		{
			DOAfterheat(num, num2, sliderDuration);
		}
	}

	public void UpdateShieldSlider(int current, int maxHealth)
	{
		shieldOutline.gameObject.SetActive(current > 0);
		if (current > 0)
		{
			shieldOutline.DOKill();
			float num = base.BarRectTransform.rect.width / (float)maxHealth;
			Vector2 sizeDelta = shieldOutline.sizeDelta;
			sizeDelta.x = num * (float)current + 20f;
			shieldOutline.DOSizeDelta(sizeDelta, sliderDuration).SetEase(Ease.OutQuad);
		}
	}

	private void UpdateCachedHealth(int value)
	{
		cachedMaxHealth = value;
	}

	private void PlayMaxIncrease(Vector2 baseSize, int current, int maxWithDark, int realMax)
	{
		float normalizedDark = 1f - (float)maxWithDark / (float)realMax;
		Difficulty difficulty = Game.Instance.SaveData.Difficulty;
		float num = baseSize.x / (40f * Game.DataManager.GetCurrentDifficultyStat(StatType.Warmth.ToString(), difficulty));
		ParticleSystem particleSystem = increaseParticle.particles[0];
		ParticleSystem.ShapeModule shape = particleSystem.shape;
		Vector3 scale = shape.scale;
		scale.x = num * (float)realMax / 100f;
		shape.scale = scale;
		ParticleSystem.EmissionModule emission = particleSystem.emission;
		for (int i = 0; i < emission.burstCount; i++)
		{
			ParticleSystem.Burst burst = emission.GetBurst(i);
			burst.count = new ParticleSystem.MinMaxCurve(GetEmissionCount(scale.x));
			emission.SetBurst(i, burst);
		}
		increaseParticle.Play();
		increaseParticle.particles[0].SetLoop(enabled: true);
		PlayIncreaseEvent();
		float fixedDeltaTime = Time.fixedDeltaTime;
		float duration = Mathf.Min((num * (float)(realMax + 5) - base.BarRectTransform.sizeDelta.x) / 3f * fixedDeltaTime, 5f);
		maxSequence = DOTween.Sequence().Append(increaseImage.DOAlpha(1f, fixedDeltaTime * 10f)).Join(base.BarRectTransform.DOSizeDelta(new Vector2(num * (float)(realMax + 5), base.BarRectTransform.rect.height), duration))
			.AppendCallback(delegate
			{
				increaseParticle.particles[0].SetLoop(enabled: false);
				StopIncreaseEvent();
			})
			.Append(base.BarRectTransform.DOSizeDelta(new Vector2(num * (float)realMax, base.BarRectTransform.rect.height), fixedDeltaTime * 10f))
			.Join(increaseImage.DOAlpha(0f, fixedDeltaTime * 10f))
			.AppendCallback(delegate
			{
				UpdateSlider(current, maxWithDark, realMax);
			});
		maxSequence.OnUpdate(delegate
		{
			SetDarkBar(normalizedDark);
		});
		maxSequence.Play();
		static float GetEmissionCount(float scaleX)
		{
			if (scaleX <= 11f)
			{
				if (scaleX <= 7f)
				{
					return 100f;
				}
				return 150f;
			}
			if (scaleX <= 15f)
			{
				return 200f;
			}
			return 200f;
		}
	}

	private void UpdateSliderOnDamaged(float normalizedHealth, float normalizedHealthWithHeat)
	{
		ResetTween();
		damageFillImage.SetAlpha(1f);
		damageSequence = DOTween.Sequence();
		damageSequence.Append(DOAfterheat(normalizedHealth, normalizedHealthWithHeat, 0.08f)).Join(fillImage.DOFillAmount(normalizedHealth, sliderDuration * 0.8f).SetEase(Ease.OutQuad)).AppendInterval(0.05f)
			.Append(damageFillImage.DOFillAmount(normalizedHealth, 0.08f).SetEase(Ease.OutQuad))
			.AppendCallback(delegate
			{
				damageFillImage.SetAlpha(0f);
			});
		damageSequence.Play();
	}

	private void UpdateDarkBarSlider(int maxWithDark, int realMax)
	{
		float num = 1f - (float)maxWithDark / (float)realMax;
		if (Mathf.Approximately(num, targetNormalizedDarkValue))
		{
			if (!darkSequence.IsActive())
			{
				SetDarkBar(targetNormalizedDarkValue);
			}
			return;
		}
		if (targetNormalizedDarkValue <= 0f && num > 0f)
		{
			darkActivateSequence?.Kill();
			darkActivateSequence = DODarkColorOnActivate();
		}
		darkSequence?.Kill();
		darkSequence = DODarkBar(num);
		darkTextureSequence?.Kill();
		darkTextureSequence = DODarkTexture(num);
		targetNormalizedDarkValue = num;
	}

	private Sequence DODarkBar(float endValue)
	{
		float normalizedDarkValue = GetNormalizedDarkValue();
		if (endValue < normalizedDarkValue)
		{
			SetDarkBar(endValue);
			return null;
		}
		return DOTween.Sequence().Append(DOTween.To(GetNormalizedDarkValue, SetDarkBar, endValue + 2f / base.BarRectTransform.rect.width, 2f / 15f).SetEase(Ease.InOutSine)).Append(DOTween.To(GetNormalizedDarkValue, SetDarkBar, endValue, 1f / 60f));
	}

	private Sequence DODarkTexture(float endValue)
	{
		float a = ((endValue <= 0f) ? 0f : 0.3f);
		return DOTween.Sequence().Append(DOPivotColor(darkTexture, new Color(0f, 0f, 0f, 0.0625f), 0.1f, exceptR: true, exceptG: true, exceptB: true)).Append(DOPivotColor(darkTexture, new Color(0f, 0f, 0f, a), 1f / 15f, exceptR: true, exceptG: true, exceptB: true));
	}

	private Sequence DOAfterheat(float normalizedHealth, float normalizedHealthWithHeat, float duration)
	{
		RectTransform afterheatRect = fillFollowerImage.rectTransform;
		Sequence sequence = DOTween.Sequence();
		sequence.Append(DOTween.To(() => afterheatRect.offsetMin.x, delegate(float x)
		{
			afterheatRect.offsetMin = new Vector2(x, afterheatRect.offsetMin.y);
		}, healthRect.rect.width * normalizedHealth, duration)).Join(DOTween.To(() => afterheatRect.offsetMax.x, delegate(float x)
		{
			afterheatRect.offsetMax = new Vector2(x, afterheatRect.offsetMax.y);
		}, (0f - healthRect.rect.width) * (1f - normalizedHealthWithHeat), duration));
		return sequence;
	}

	private float GetNormalizedDarkValue()
	{
		float width = base.BarRectTransform.rect.width;
		return (darkRect.sizeDelta.x - 19f) / width;
	}

	private void SetHealthRect(float width)
	{
		healthRect.sizeDelta = new Vector2(width, healthRect.sizeDelta.y);
		RectTransform rectTransform = fillFollowerImage.rectTransform;
		rectTransform.offsetMin = new Vector2(width * fillImage.fillAmount, rectTransform.offsetMin.y);
	}

	private void SetDarkBar(float normalizedDarkValue)
	{
		float width = base.BarRectTransform.rect.width;
		float num = width * (1f - normalizedDarkValue);
		SetHealthRect(num);
		float x = width * normalizedDarkValue + 19f;
		float x2 = 10f + num;
		darkRect.anchoredPosition = new Vector2(x2, darkRect.anchoredPosition.y);
		darkRect.sizeDelta = new Vector2(x, darkRect.sizeDelta.y);
		darkRect.gameObject.SetActive(normalizedDarkValue > 0f);
	}

	private Sequence DODarkColorOnActivate()
	{
		return DOTween.Sequence().AppendInterval(1f / 6f).Append(darkLine.DOAlpha(0.4f, 1f / 12f))
			.Insert(0f, DOPivotColor(darkTexture, Color.white, 1f / 3f, exceptR: false, exceptG: false, exceptB: false, exceptA: true));
	}

	private Tween DOPivotColor(UnityEngine.UI.Graphic graphic, Color endValue, float duration, bool exceptR = false, bool exceptG = false, bool exceptB = false, bool exceptA = false)
	{
		return DOTween.To(() => graphic.color, delegate(Color color)
		{
			Color color2 = graphic.color;
			if (!exceptR)
			{
				color2.r = color.r;
			}
			if (!exceptG)
			{
				color2.g = color.g;
			}
			if (!exceptB)
			{
				color2.b = color.b;
			}
			if (!exceptA)
			{
				color2.a = color.a;
			}
			graphic.color = color2;
		}, endValue, duration);
	}

	private void ResetTween()
	{
		damageSequence?.Kill();
		fillImage.DOKill();
		fillFollowerImage.DOKill();
		base.BarRectTransform.DOKill();
		damageFillImage.DOKill();
	}
}
