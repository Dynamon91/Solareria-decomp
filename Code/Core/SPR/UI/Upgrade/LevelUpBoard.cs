using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Upgrade;

public class LevelUpBoard : UpgradeBoard<LevelUpModel>
{
	private class InputAccelerator
	{
		private const float InputChainTimeLimit = 0.1f;

		private const float InitialAccelTime = 2f;

		private const float AccelInterval = 3f;

		private float currentTime = -999f;

		private float lastInputTime = -999f;

		private float chainInputStartTime = -999f;

		private readonly List<int> accelerateMultipliers;

		public InputAccelerator(List<int> accelerateMultipliers)
		{
			this.accelerateMultipliers = accelerateMultipliers;
		}

		private void Tick()
		{
			currentTime = Time.time;
			float num = currentTime - lastInputTime;
			lastInputTime = currentTime;
			if (num > 0.1f)
			{
				chainInputStartTime = -999f;
			}
			else if (chainInputStartTime < 0f)
			{
				chainInputStartTime = currentTime;
			}
		}

		public int GetMultiplier()
		{
			Tick();
			float num = currentTime - chainInputStartTime;
			if (chainInputStartTime < 0f || num < 2f)
			{
				return 1;
			}
			num -= 2f;
			int a = ((num < 0f) ? (-1) : Mathf.FloorToInt(num / 3f));
			a = Mathf.Min(a, accelerateMultipliers.Count - 1);
			return Mathf.Max(1, accelerateMultipliers[a]);
		}
	}

	[Header("Level Up")]
	[SerializeField]
	private Image currentExpBar;

	[SerializeField]
	private TextMeshProUGUI beforeLevelText;

	[SerializeField]
	private TextMeshProUGUI afterLevelText;

	[SerializeField]
	private RectTransform expLeftArrow;

	[SerializeField]
	private RectTransform expRightArrow;

	[SerializeField]
	private TextMeshProUGUI purchaseExpText;

	[Header("Cost Group")]
	[SerializeField]
	private GameObject costGroup;

	[Header("Warning Texts")]
	[SerializeField]
	private TextMeshProUGUI maximumLevelText;

	[Header("Deprecated")]
	[SerializeField]
	private List<int> accelerateMultipliers;

	private List<int> requiredExpPerLevel;

	private Sequence arrowMoveSequence;

	public override UniTask InitializeAsync()
	{
		currentExpBar.fillAmount = 0f;
		requiredExpPerLevel = CalculateLevelAndExp();
		return UniTask.CompletedTask;
	}

	protected override void UpdateUI()
	{
		int targetLevel = currentModel.TargetLevel;
		int level = UpgradeBoard<LevelUpModel>.PlayerStat.Level;
		int levelDelta = currentModel.LevelDelta;
		int addExp = requiredExpPerLevel[levelDelta];
		int money = UpgradeBoard<LevelUpModel>.PlayerData.Money;
		int upgradeCost = GetUpgradeCost(addExp);
		beforeLevelText.text = LevelText(level);
		afterLevelText.text = LevelText(targetLevel);
		purchaseExpText.text = addExp.ToString();
		expLeftArrow.gameObject.SetActive(levelDelta > 0);
		expRightArrow.gameObject.SetActive(targetLevel < 120);
		if (currentModel.ShouldIncrease.HasValue)
		{
			bool value = currentModel.ShouldIncrease.Value;
			TweenArrow(value);
		}
		UpdateCostText(upgradeCost, money);
		UpgradeCheckResult canProceedUpgrade = CanProceedUpgrade();
		UpdateFeedbackText(canProceedUpgrade, onProceed: false);
	}

	public override UpgradeCheckResult CanProceedUpgrade()
	{
		if (requiredExpPerLevel.Count == 1)
		{
			return UpgradeCheckResult.MaximumUpgrade;
		}
		if (currentModel.LevelDelta == 0)
		{
			return UpgradeCheckResult.SameGrade;
		}
		int addExp = requiredExpPerLevel[currentModel.LevelDelta];
		int money = UpgradeBoard<LevelUpModel>.PlayerData.Money;
		int upgradeCost = GetUpgradeCost(addExp);
		if (money < upgradeCost)
		{
			return UpgradeCheckResult.NotEnoughOhn;
		}
		return UpgradeCheckResult.Success;
	}

	public override void OnProceedUpgrade()
	{
		int num = requiredExpPerLevel[currentModel.LevelDelta];
		UpgradeBoard<LevelUpModel>.PlayerStat.AddExperience(num);
		int upgradeCost = GetUpgradeCost(num);
		UpgradeBoard<LevelUpModel>.PlayerStat.AddMoney(-upgradeCost);
	}

	public override void OnBlockUpgrade(UpgradeCheckResult ucResult)
	{
		UpdateFeedbackText(ucResult, onProceed: true);
	}

	protected override void ShowRequirements(bool value)
	{
		costGroup.SetActive(value);
		maximumLevelText.gameObject.SetActive(!value);
	}

	private List<int> CalculateLevelAndExp()
	{
		List<int> list = new List<int> { 0 };
		int level = UpgradeBoard<LevelUpModel>.PlayerStat.Level;
		int currentExp = UpgradeBoard<LevelUpModel>.PlayerStat.CurrentExp;
		while (level + list.Count <= 120)
		{
			int count = list.Count;
			int item = list[count - 1] + PlayerStat.GetMaxExperienceOf(level + count - 1) - ((count == 1) ? currentExp : 0);
			list.Add(item);
		}
		return list;
	}

	private void TweenArrow(bool isIncrease)
	{
		arrowMoveSequence.Kill(complete: true);
		RectTransform rectTransform = (isIncrease ? expRightArrow : expLeftArrow);
		int num = (isIncrease ? 5 : (-5));
		float x = rectTransform.anchoredPosition.x;
		arrowMoveSequence = DOTween.Sequence();
		arrowMoveSequence.Append(rectTransform.DOAnchorPosX(x + (float)num, 1f / 6f)).Append(rectTransform.DOAnchorPosX(x, 1f / 12f));
	}

	private int GetUpgradeCost(int addExp)
	{
		return 5 * addExp;
	}

	private string LevelText(int level)
	{
		return $"{level}";
	}

	public void ShowCurrentExpRatio()
	{
		currentExpBar.DOKill();
		currentExpBar.fillAmount = 0f;
		int currentExp = UpgradeBoard<LevelUpModel>.PlayerStat.CurrentExp;
		int maxExp = UpgradeBoard<LevelUpModel>.PlayerStat.MaxExp;
		float num = (float)currentExp / (float)maxExp;
		float duration = num * 25f / 60f;
		currentExpBar.DOFillAmount(num, duration).SetEase(Ease.OutQuad);
	}

	public void FillExpRatio()
	{
		currentExpBar.DOKill();
		currentExpBar.DOFillAmount(1f, 1f / 3f).SetEase(Ease.OutQuad);
	}
}
