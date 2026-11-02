using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SPR.UI.Trait;

public class TraitStatusCardElement : TraitCardElement
{
	[SerializeField]
	private bool disableAnimation;

	[SerializeField]
	private TextMeshProUGUI levelText;

	[SerializeField]
	private Transform backParent;

	[SerializeField]
	private GameObject rank1;

	[SerializeField]
	private GameObject rank2;

	[SerializeField]
	private GameObject rank3;

	private int level;

	private int levelForShow;

	private int diff;

	private Sequence seq;

	private static readonly int Open = Animator.StringToHash("Open");

	private static readonly int Close = Animator.StringToHash("Close");

	public int DifferedLevel => level + diff;

	public int DifferedLevelForShow => levelForShow + diff;

	public int Rank { get; private set; }

	public string CurrentText => levelText.text;

	public override void RefreshData()
	{
		diff = 0;
		if (TraitCardElement.PlayerData.TryGetCardData(base.ID, out var cardData))
		{
			Rank = cardData.Rank;
			level = cardData.RawLevel;
			levelForShow = cardData.CurrentLevel;
			ShowLevelText(levelForShow);
		}
		else
		{
			levelText.text = string.Empty;
			Rank = 0;
		}
		SetIconImage(rank1);
		rank1.SetActive(Rank >= 1);
		rank2.SetActive(Rank >= 2);
		rank3.SetActive(Rank >= 3);
		for (int i = 0; i < 3; i++)
		{
			backParent.GetChild(i).gameObject.SetActive(i == Rank - 1);
		}
		ShowAnimation();
	}

	private void ShowLevelText(int value)
	{
		levelText.text = value.ToString();
		int num = diff;
		Color color = ((num > 0) ? MenuTraitPanel.IncreaseColor : ((num >= 0) ? MenuTraitPanel.DefaultColor : MenuTraitPanel.DecreaseColor));
		Color endValue = color;
		seq?.Kill();
		seq = DOTween.Sequence().Join(levelText.DOColor(endValue, 1f / 6f).SetEase(Ease.Linear));
	}

	private void ShowAnimation()
	{
		if (disableAnimation)
		{
			base.Animator.runtimeAnimatorController = null;
		}
		else if (base.gameObject.activeInHierarchy && !IsPlayingAnimation)
		{
			bool flag = diff != 0;
			base.Animator.ResetTrigger(flag ? Close : Open);
			base.Animator.SetTrigger(flag ? Open : Close);
		}
	}

	public TraitResult TryAddDiff(int tryDiff, int diffSum, out int currentDiff)
	{
		currentDiff = 0;
		if (tryDiff > 0 && TraitCardElement.PlayerData.AvailableTraitPoint - diffSum <= 0)
		{
			return TraitResult.LackOfPoint;
		}
		if (DifferedLevel + tryDiff < 0)
		{
			return TraitResult.LowerThanZero;
		}
		if (DifferedLevel + tryDiff > Game.DataManager.GetTraitData(base.ID).MaxLevel)
		{
			return TraitResult.IsFullRank;
		}
		diff += tryDiff;
		currentDiff = diff;
		ShowLevelText(DifferedLevelForShow);
		ShowAnimation();
		return TraitResult.Success;
	}

	public void ApplyLevel()
	{
		if (Rank != 0)
		{
			TraitCardElement.PlayerData.SetCardLevel(base.ID, DifferedLevel);
			RefreshData();
		}
	}

	public void ResetLevel()
	{
		if (Rank != 0)
		{
			int num = diff;
			TraitCardElement.PlayerData.SetCardLevel(base.ID, 0);
			RefreshData();
			base.Animator.ResetTrigger(Open);
			base.Animator.ResetTrigger(Close);
			if (num == 0)
			{
				base.Animator.SetTrigger(Open);
			}
			base.Animator.SetTrigger(Close);
		}
	}
}
