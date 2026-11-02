using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Field;

public class StatusEffectIcon : MonoBehaviour
{
	[SerializeField]
	protected Image effectImage;

	[SerializeField]
	private Image radialFillImage;

	[SerializeField]
	private Image maskBackgroundImage;

	[SerializeField]
	private TextMeshProUGUI countText;

	protected StatusEffect StatusEffect;

	private RectTransform rect;

	private CanvasGroup canvasGroup;

	private Sequence hideSequence;

	public static readonly Dictionary<StatusEffectType, Color32> EffectKeyColor = new Dictionary<StatusEffectType, Color32>
	{
		{
			StatusEffectType.Flame,
			new Color32(byte.MaxValue, 66, 54, byte.MaxValue)
		},
		{
			StatusEffectType.Electric,
			new Color32(byte.MaxValue, 158, 0, byte.MaxValue)
		},
		{
			StatusEffectType.Spore,
			new Color32(179, 83, byte.MaxValue, byte.MaxValue)
		},
		{
			StatusEffectType.Wet,
			new Color32(0, 118, byte.MaxValue, byte.MaxValue)
		}
	};

	private RectTransform Rect => rect ?? (rect = GetComponent<RectTransform>());

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	protected static EffectBoard EffectBoard => Game.Instance.UIManager.FieldCanvas.PlayerStatusBoard.EffectBoard;

	public bool IsValid => StatusEffect != null;

	public StatusEffectType EffectType => StatusEffect.EffectType;

	public virtual void StartEffect(StatusEffect statusEffect)
	{
		StatusEffect = statusEffect;
		LoadAndSetSpriteAsync().Forget();
		hideSequence?.Kill();
		Rect.anchoredPosition = Vector2.zero;
		CanvasGroup.alpha = 1f;
		maskBackgroundImage.color = Color.white;
		radialFillImage.color = EffectKeyColor[statusEffect.EffectType];
		base.gameObject.SetActive(value: true);
	}

	public virtual void TickEffect()
	{
		countText.text = StatusEffect.StackCount.ToString();
		countText.gameObject.SetActive(StatusEffect.StackCount > 1);
		radialFillImage.fillAmount = 1f - (float)StatusEffect.CurrentFrame / (float)StatusEffect.DurationFrame;
		if (StatusEffect.IsDone)
		{
			HideIconWithHighlight();
		}
	}

	public void HideIconWithHighlight(bool instant = false)
	{
		hideSequence?.Kill();
		if (instant)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		hideSequence = DOTween.Sequence();
		hideSequence.Append(CanvasGroup.DOAlpha(0f, 0.25f).SetEase(Ease.Linear)).Join(maskBackgroundImage.DOAlpha(0f, 0.25f).SetEase(Ease.Linear)).OnComplete(delegate
		{
			base.gameObject.SetActive(value: false);
		});
	}

	private async UniTask LoadAndSetSpriteAsync()
	{
		UniTask<Sprite> statusEffectSpriteAsync = EffectBoard.GetStatusEffectSpriteAsync(StatusEffect.EffectType, StatusEffect.StackCount);
		Image image = effectImage;
		image.sprite = await statusEffectSpriteAsync;
	}
}
