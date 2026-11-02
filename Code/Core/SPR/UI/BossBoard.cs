using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI;

public class BossBoard : MonoBehaviour
{
	[SerializeField]
	private LocaleText nameText;

	[SerializeField]
	private EffectSlider hpSlider;

	[SerializeField]
	private GameObject burstIcon;

	[SerializeField]
	private GameObject executeLine;

	[SerializeField]
	private StunPointGroup stunPointGroup;

	[SerializeField]
	private NarrationBoard bossSpeech;

	[SerializeField]
	private EffectSlider regainHpSlider;

	private RectTransform rect;

	private CanvasGroup canvasGroup;

	private Sequence showHideSeq;

	private readonly Vector2 hidePosition = new Vector2(0f, -40f);

	private readonly Vector2 showPosition = new Vector2(0f, 0f);

	private const float HideScale = 1.1f;

	public NarrationBoard BossSpeech => bossSpeech;

	private RectTransform Rect => rect ?? (rect = GetComponent<RectTransform>());

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	public void Initialize()
	{
		nameText.ClearText();
		hpSlider.Initialize();
		CanvasGroup.alpha = 0f;
		burstIcon.SetActive(value: false);
		ActivateExecuteLine(value: false);
		base.gameObject.SetActive(value: false);
		UpdateRegainHealthBar(0, 0, 0);
	}

	public void SetBoss(Enemy enemy)
	{
		string text = ((!(enemy is BossCharacter bossCharacter)) ? enemy.Name : ((bossCharacter.BossName == BossName.Imahen) ? "ImahenCoreMachine" : bossCharacter.BossName.ToString()));
		nameText.SetData("Boss_" + text);
		stunPointGroup.SetCount(StunPointGroup.Type.Boss, enemy.MaxStunCount).Forget();
		stunPointGroup.UpdateCount(enemy.CurrentStun);
		ActivateExecuteLine(enemy.HasStatusEffect<ExecutionStatusEffect>());
		base.gameObject.SetActive(value: true);
		UpdateHealthBar(enemy.Health, enemy.MaxHealth, 0);
	}

	public async UniTask ShowAsync(float duration = 0.6f)
	{
		showHideSeq?.Kill();
		if (!(Math.Abs(CanvasGroup.alpha - 1f) < 0.01f) && base.gameObject.activeSelf)
		{
			showHideSeq = DOTween.Sequence();
			showHideSeq.Append(Rect.DOAnchorPos(showPosition, duration)).Join(Rect.DOScale(Vector3.one, duration)).Join(CanvasGroup.DOFade(1f, duration));
			await showHideSeq.WaitAsync();
		}
	}

	public async UniTask HideAsync(float duration = 0.6f, bool deactivate = false)
	{
		showHideSeq?.Kill();
		if (base.gameObject.activeSelf)
		{
			if (Mathf.Abs(CanvasGroup.alpha) > 0.01f)
			{
				showHideSeq = DOTween.Sequence();
				showHideSeq.Append(Rect.DOAnchorPos(hidePosition, duration)).Join(Rect.DOScale(Vector3.one * 1.1f, duration)).Join(CanvasGroup.DOFade(0f, duration));
				await showHideSeq.WaitAsync();
			}
			if (deactivate)
			{
				base.gameObject.SetActive(value: false);
			}
		}
	}

	public void UpdateHealthBar(int current, int max, int delta)
	{
		hpSlider.UpdateEnemySlider(current, max, delta);
	}

	public void UpdateRegainHealthBar(int value, int max, int delta)
	{
		regainHpSlider.gameObject.SetActive(value > 0);
		if (value != 0)
		{
			float num = hpSlider.BarRectTransform.rect.width - 10f;
			float x = num * hpSlider.FillAmount + 10f;
			float x2 = num * (1f - hpSlider.FillAmount);
			regainHpSlider.BarRectTransform.anchoredPosition = new Vector2(x, regainHpSlider.BarRectTransform.anchoredPosition.y);
			regainHpSlider.BarRectTransform.sizeDelta = new Vector2(x2, regainHpSlider.BarRectTransform.sizeDelta.y);
			regainHpSlider.UpdateEnemySlider(value, max, 1);
		}
	}

	public void PlayHealTween(bool loop, Type subscriberType)
	{
		hpSlider.PlayHealTween(loop, subscriberType);
	}

	public void StopHealTween(Type subscriberType)
	{
		hpSlider.StopHealTween(subscriberType);
	}

	public void ActivateBurstIcon(bool value, bool isClicked)
	{
		if (!burstIcon.TryGetComponent<Animator>(out var component))
		{
			return;
		}
		if (value)
		{
			if (!isClicked)
			{
				burstIcon.SetActive(value: true);
				component.Play("highlight");
			}
			else
			{
				DisableExecuteIcon(component).Forget();
			}
		}
		else
		{
			burstIcon.SetActive(value: false);
		}
	}

	public void ActivateExecuteLine(bool value)
	{
		executeLine.SetActive(value);
	}

	public void UpdateStunPoint(int value)
	{
		stunPointGroup.UpdateCount(value);
	}

	public void ResetStunPoint()
	{
		stunPointGroup.ResetCount();
	}

	private async UniTaskVoid DisableExecuteIcon(Animator animator)
	{
		animator.Play("disable");
		await UniTask.WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("disable") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.99f);
		burstIcon.SetActive(value: false);
	}
}
