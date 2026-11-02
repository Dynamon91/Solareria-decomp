using DG.Tweening;
using Library;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Trait;

public class TraitStatusDetailElement : MonoBehaviour
{
	private const float Duration = 1f / 6f;

	[SerializeField]
	private StatType type;

	[SerializeField]
	private LocaleText nameText;

	[SerializeField]
	private TextMeshProUGUI valueText;

	[SerializeField]
	private Image background;

	[SerializeField]
	private Image diamond;

	[SerializeField]
	[Range(0f, 1f)]
	private float backgroundAlpha;

	[SerializeField]
	private CanvasGroup cursorGroup;

	private Sequence setSeq;

	private Sequence focusSeq;

	private Sequence cursorSeq;

	private RectTransform rectTransform;

	public RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	public StatType Type => type;

	private static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	public void Initialize()
	{
		nameText.SetData($"Stat_{type}");
	}

	public void ResetData()
	{
		valueText.text = PlayerStat.GetStat(Type).GetPrintText();
		Color defaultColor = MenuTraitPanel.DefaultColor;
		nameText.SetColor(defaultColor);
		valueText.color = defaultColor;
	}

	public void SetChange(bool changed, float value = 0f)
	{
		Stat stat = PlayerStat.GetStat(Type);
		int num = (changed ? 1 : 0);
		float currentValue = stat.GetCurrentValue();
		float previewValue = stat.GetPreviewValue(value);
		Color endValue = ((previewValue > currentValue) ? MenuTraitPanel.IncreaseColor : ((previewValue < currentValue) ? MenuTraitPanel.DecreaseColor : MenuTraitPanel.DefaultColor));
		Ease ease = MenuTraitPanel.GetEase(changed);
		valueText.text = PlayerStat.GetStat(Type).GetPrintText(changed ? value : (-1f));
		setSeq?.Kill();
		setSeq = DOTween.Sequence();
		setSeq.Join(diamond.DOAlpha(num, 1f / 6f).SetEase(ease)).Join(valueText.DOColor(endValue, 1f / 6f).SetEase(Ease.Linear)).Join(DOTween.To(() => nameText.GetColor(), delegate(Color c)
		{
			nameText.SetColor(c);
		}, endValue, 1f / 6f).SetEase(Ease.Linear));
	}

	public void SetFocus(bool focus, bool instant = false)
	{
		float num = (focus ? backgroundAlpha : 0f);
		if (instant)
		{
			background.SetAlpha(num);
			return;
		}
		Ease ease = MenuTraitPanel.GetEase(focus);
		focusSeq?.Kill();
		focusSeq = DOTween.Sequence().Join(background.DOAlpha(num, 1f / 6f).SetEase(ease));
	}

	public void SetCursor(bool value, bool instant = false)
	{
		int num = (value ? 1 : 0);
		if (instant)
		{
			cursorGroup.alpha = num;
			return;
		}
		Ease ease = MenuTraitPanel.GetEase(!value);
		cursorSeq?.Kill();
		cursorSeq = DOTween.Sequence().Join(cursorGroup.DOAlpha(num, 1f / 6f).SetEase(ease));
	}
}
