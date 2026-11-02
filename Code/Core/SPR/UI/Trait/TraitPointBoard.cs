using DG.Tweening;
using Library;
using SPR.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Trait;

public class TraitPointBoard : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI pointText;

	[SerializeField]
	private Image white;

	private CanvasGroup canvasGroup;

	private int diff;

	private Sequence fadeSeq;

	private Sequence messageSeq;

	private static readonly Color DefaultColor = new Color(1f, 0f, 0f, 0f);

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	public int AvailablePoint => PlayerData.AvailableTraitPoint + diff;

	public void SetData()
	{
		RefreshText();
	}

	public void SetActive(bool value, bool instant = false)
	{
		int num = (value ? 1 : 0);
		if (instant)
		{
			CanvasGroup.alpha = num;
		}
		else
		{
			fadeSeq?.Kill();
			fadeSeq = DOTween.Sequence().Join(CanvasGroup.DOAlpha(num, 1f / 6f).SetEase(Ease.Linear));
		}
		RefreshText();
	}

	private void RefreshText()
	{
		pointText.text = AvailablePoint.ToString();
	}

	public void SetDiff(int diff)
	{
		this.diff = diff;
		RefreshText();
	}

	public void ShowColor()
	{
		Color messageColor = new Color(1f, 0f, 0f, 4f / 51f);
		white.color = DefaultColor;
		messageSeq?.Kill();
		messageSeq = DOTween.Sequence().AppendInterval(1f).OnStart(delegate
		{
			white.color = messageColor;
		})
			.OnComplete(delegate
			{
				white.color = DefaultColor;
			});
	}
}
