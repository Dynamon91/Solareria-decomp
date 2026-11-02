using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI.Trait;

public class TraitStatusDetailInfoBoard : MonoBehaviour
{
	[SerializeField]
	private LocaleText nameText;

	[SerializeField]
	private LocaleText descText;

	private CanvasGroup canvasGroup;

	private Sequence seq;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	public void SetActive(bool value, bool instant = false)
	{
		float num = (value ? 1f : 0f);
		if (instant)
		{
			CanvasGroup.alpha = num;
			return;
		}
		seq?.Kill();
		seq = DOTween.Sequence().Join(CanvasGroup.DOAlpha(num, 1f / 6f).SetEase(Ease.Linear));
	}

	public void SetData(StatType type)
	{
		nameText.SetData($"Stat_{type}");
		descText.SetData($"Stat_{type}_Desc");
	}
}
