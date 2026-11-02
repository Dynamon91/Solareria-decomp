using Cysharp.Threading.Tasks;
using DG.Tweening;
using SPR.Library;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Field;

public class StatusEffectStackIcon : StatusEffectIcon
{
	[Header("Electric Icon")]
	[SerializeField]
	private Transform lineTransform;

	[SerializeField]
	private Image animSquareImage;

	private Sequence lineSequence;

	private Sequence iconSequence;

	private Sequence squareSequence;

	private int previousStackCount;

	private int previousCurrentFrame;

	private readonly Vector3 smallLineScale = new Vector3(0.9f, 0.9f, 1f);

	public override void StartEffect(StatusEffect statusEffect)
	{
		base.StartEffect(statusEffect);
		lineSequence?.Kill();
		lineTransform.localScale = Vector3.one;
		lineSequence = TweenLine(20);
		animSquareImage.color = GraphicHelper.TransparentColor;
	}

	public override void TickEffect()
	{
		base.TickEffect();
		ControlLineTransform();
		ControlIconSpriteAsync().Forget();
		ControlAnimSquare();
		previousStackCount = StatusEffect.StackCount;
		previousCurrentFrame = StatusEffect.CurrentFrame;
	}

	private void ControlLineTransform()
	{
		if (IsStacked())
		{
			lineSequence?.Kill();
			lineTransform.localScale = Vector3.one;
			lineSequence = TweenLine(20);
		}
		else if (StatusEffect.CurrentFrame == Mathf.RoundToInt((float)StatusEffect.DurationFrame * 0.75f))
		{
			lineSequence?.Kill();
			lineTransform.localScale = Vector3.one;
			lineSequence = TweenLine(10);
		}
		else
		{
			if (StatusEffect.StackCount >= previousStackCount)
			{
				return;
			}
			lineSequence?.Kill();
			lineTransform.localScale = smallLineScale;
			DOTween.Sequence().AppendInterval(1f / 6f).OnComplete(delegate
			{
				if (StatusEffect != null)
				{
					lineSequence?.Kill();
					lineTransform.localScale = Vector3.one;
					lineSequence = TweenLine(20);
				}
			});
		}
	}

	private async UniTask ControlIconSpriteAsync()
	{
		if (IsStacked())
		{
			iconSequence?.Kill();
			Image image = effectImage;
			image.sprite = await StatusEffectIcon.EffectBoard.GetStatusEffectSpriteAsync(StatusEffect.EffectType, StatusEffect.StackCount);
		}
		else if (StatusEffect.StackCount < previousStackCount && StatusEffect.StackCount > 0)
		{
			Sprite activeSprite = await StatusEffectIcon.EffectBoard.GetStatusEffectSpriteAsync(StatusEffect.EffectType, previousStackCount, active: true);
			Sprite currentStackSprite = await StatusEffectIcon.EffectBoard.GetStatusEffectSpriteAsync(StatusEffect.EffectType, StatusEffect.StackCount);
			effectImage.sprite = activeSprite;
			iconSequence?.Kill();
			iconSequence = DOTween.Sequence();
			iconSequence.AppendInterval(1f / 6f).OnComplete(delegate
			{
				effectImage.sprite = currentStackSprite;
			});
		}
		else if (StatusEffect.StackCount < previousStackCount && StatusEffect.StackCount == 0)
		{
			iconSequence?.Kill();
			Sprite sprite = await StatusEffectIcon.EffectBoard.GetStatusEffectSpriteAsync(StatusEffect.EffectType, previousStackCount, active: true);
			effectImage.sprite = sprite;
		}
	}

	private void ControlAnimSquare()
	{
		if (IsStacked())
		{
			squareSequence?.Kill(complete: true);
		}
		else if (StatusEffect.CurrentFrame == StatusEffect.DurationFrame - 5)
		{
			animSquareImage.transform.localScale = 0.5f * Vector3.one;
			Color32 color = StatusEffectIcon.EffectKeyColor[StatusEffect.EffectType];
			color.a = 0;
			animSquareImage.color = color;
			squareSequence?.Kill();
			squareSequence = DOTween.Sequence();
			squareSequence.Append(animSquareImage.DOColor(Color.white, 1f / 12f).SetEase(Ease.Linear)).Join(animSquareImage.transform.DOScale(0.9f * Vector3.one, 1f / 12f).SetEase(Ease.Linear));
			if (StatusEffect.StackCount > 1)
			{
				squareSequence.Append(animSquareImage.DOColor(GraphicHelper.TransparentColor, 0.25f).SetEase(Ease.Linear)).Join(animSquareImage.transform.DOScale(1.1f * Vector3.one, 0.25f).SetEase(Ease.Linear));
			}
		}
	}

	private Sequence TweenLine(int halfDurationFrame)
	{
		float duration = (float)halfDurationFrame / 60f;
		Sequence sequence = DOTween.Sequence();
		sequence.Append(lineTransform.DOScale(smallLineScale, duration).SetEase(Ease.Linear)).Append(lineTransform.DOScale(Vector3.one, duration).SetEase(Ease.Linear)).SetLoops(-1);
		return sequence;
	}

	private bool IsStacked()
	{
		bool num = StatusEffect.StackCount > previousStackCount;
		bool flag = StatusEffect.StackCount == previousStackCount && StatusEffect.CurrentFrame < previousCurrentFrame;
		return num || flag;
	}
}
