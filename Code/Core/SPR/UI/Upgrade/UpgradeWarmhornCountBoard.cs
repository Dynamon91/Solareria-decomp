using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR.UI.Upgrade;

public class UpgradeWarmhornCountBoard : UpgradeBoard<UpgradeWarmhornCountModel>
{
	[Header("Warmhorn Count")]
	[SerializeField]
	protected ChangingStatusListBoard statusListBoard;

	[SerializeField]
	private IngredientBoard ingredientBoard;

	private const string UpgradeMaterialName = "EmptyWarmhornShell";

	public override async UniTask InitializeAsync()
	{
		await AddressableHelper.PreloadSpriteAsync(Address.ItemSprite("EmptyWarmhornShell"));
	}

	protected override void UpdateUI()
	{
		int warmhornCount = currentModel.WarmhornCount;
		List<ChangingStatusInfoBoard.StatusChangeInfo> data = new List<ChangingStatusInfoBoard.StatusChangeInfo>
		{
			new ChangingStatusInfoBoard.StatusChangeInfo(StatType.WarmhornCount, warmhornCount, 1f, isIncreaseBeneficial: true)
		};
		int money = UpgradeBoard<UpgradeWarmhornCountModel>.PlayerStat.Money;
		int upgradeCostOf = GetUpgradeCostOf();
		statusListBoard.SetData(data);
		UpdateCostText(upgradeCostOf, money);
		ingredientBoard.SetData("EmptyWarmhornShell", 1);
	}

	public override UpgradeCheckResult CanProceedUpgrade()
	{
		UpgradeBoard<UpgradeWarmhornCountModel>.PlayerData.HasEverObtainedItem("EmptyWarmhornShell", out var current);
		if (current < 1)
		{
			return UpgradeCheckResult.NotEnoughMaterial;
		}
		int upgradeCostOf = GetUpgradeCostOf();
		if (UpgradeBoard<UpgradeWarmhornCountModel>.PlayerData.Money < upgradeCostOf)
		{
			return UpgradeCheckResult.NotEnoughOhn;
		}
		return UpgradeCheckResult.Success;
	}

	public override void OnProceedUpgrade()
	{
		UpgradeBoard<UpgradeWarmhornCountModel>.PlayerStat.GetStat<WarmhornCount>().AddMaxPotion(1);
		int upgradeCostOf = GetUpgradeCostOf();
		UpgradeBoard<UpgradeWarmhornCountModel>.PlayerStat.AddMoney(-upgradeCostOf);
		UpgradeBoard<UpgradeWarmhornCountModel>.PlayerData.TryRemoveAcquiredItem("EmptyWarmhornShell");
	}

	public override void OnBlockUpgrade(UpgradeCheckResult ucResult)
	{
		UpdateFeedbackText(ucResult, onProceed: true);
	}

	protected override void ShowRequirements(bool value)
	{
	}

	private int GetUpgradeCostOf()
	{
		return 1000;
	}
}
