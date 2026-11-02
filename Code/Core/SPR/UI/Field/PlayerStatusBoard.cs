using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI.Field;

public class PlayerStatusBoard : MonoBehaviour
{
	[SerializeField]
	private HpSlider hpSlider;

	[SerializeField]
	private SpSlider spSlider;

	[SerializeField]
	private EffectBoard effectBoard;

	[SerializeField]
	private HeatBoard heatBoard;

	[Header("게이지 설정")]
	[SerializeField]
	private Vector2 hpBaseSize;

	[SerializeField]
	private Vector2 spBaseSize;

	private RectTransform rect;

	private CanvasGroup canvasGroup;

	private Sequence showHideSequence;

	private RectTransform Rect => rect ?? (rect = GetComponent<RectTransform>());

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	private static DataManager DataManager => Game.DataManager;

	public HeatBoard HeatBoard => heatBoard;

	public EffectBoard EffectBoard => effectBoard;

	public void Initialize()
	{
		hpSlider.Initialize();
		UpdateBarSize(instant: true);
		heatBoard.Initialize(PlayerStat.HeatMaxLevel, PlayerStat.HeatGauge);
		effectBoard.Initialize();
		HideAsync(0f).Forget();
	}

	public void UpdateHealthBar(int current, int maxWithDark, int realMax, bool instant)
	{
		hpSlider.UpdateSlider(current, maxWithDark, realMax, instant);
	}

	public void UpdateShieldBar(int current, int maxHealth)
	{
		hpSlider.UpdateShieldSlider(current, maxHealth);
	}

	public void PlayHealTween(bool loop, Type subscriberType)
	{
		hpSlider.PlayHealTween(loop, subscriberType);
	}

	public void StopHealTween(Type subscriberType)
	{
		hpSlider.StopHealTween(subscriberType);
	}

	public void UpdateStaminaBar(float current, int max)
	{
		spSlider.UpdateSlider(current, max);
	}

	public void UpdateHeatBoard(int activeLevel, float gaugeCurrent)
	{
		heatBoard.UpdateData(activeLevel, gaugeCurrent);
	}

	public void UpdateBarSize(bool instant)
	{
		hpSlider.UpdateBarSize(hpBaseSize, instant);
		spSlider.UpdateBarSize(spBaseSize);
	}

	public void PlayMaxHealthChangedEffect()
	{
		hpSlider.PlayMaxChangedEffect(hpBaseSize);
	}

	public void PlayNoStaminaEffect()
	{
		spSlider.PlayNoStaminaEffect();
	}

	public async UniTask ShowAsync(float duration)
	{
		showHideSequence?.Kill();
		base.gameObject.SetActive(value: true);
		if (duration <= 0f)
		{
			Rect.localScale = Vector3.one;
			CanvasGroup.alpha = 1f;
		}
		else if (!CanvasGroup.alpha.IsAlmostSame(1f))
		{
			Rect.localScale = Vector3.one * 1.1f;
			CanvasGroup.alpha = 0f;
			showHideSequence = DOTween.Sequence();
			showHideSequence.Append(Rect.DOScale(Vector3.one, duration)).Join(CanvasGroup.DOFade(1f, duration));
			await showHideSequence.WaitAsync();
		}
	}

	public async UniTask HideAsync(float duration)
	{
		showHideSequence?.Kill();
		if (duration <= 0f)
		{
			Rect.localScale = Vector3.one * 1.1f;
			CanvasGroup.alpha = 0f;
			base.gameObject.SetActive(value: false);
		}
		else if (CanvasGroup.alpha.IsAlmostSame(0f))
		{
			base.gameObject.SetActive(value: false);
		}
		else
		{
			Rect.localScale = Vector3.one;
			CanvasGroup.alpha = 1f;
			showHideSequence = DOTween.Sequence();
			showHideSequence.Append(Rect.DOScale(Vector3.one * 1.1f, duration)).Join(CanvasGroup.DOFade(0f, duration));
			await showHideSequence.WaitAsync();
		}
	}
}
