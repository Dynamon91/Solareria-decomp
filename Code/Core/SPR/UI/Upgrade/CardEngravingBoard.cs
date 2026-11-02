using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.L10N;
using SPR.Save;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace SPR.UI.Upgrade;

public class CardEngravingBoard : UpgradeBoard<CardEngravingModel>
{
	[Header("Card Engraving")]
	[SerializeField]
	private Image iconImage;

	[SerializeField]
	private LocaleText nameText;

	[SerializeField]
	private List<CardEngravingRankBoard> engravingRankBoards;

	[SerializeField]
	private IngredientBoard ingredientBoard;

	[SerializeField]
	private LocaleText maximumUpgradeText;

	private string lastMaterialName;

	public override async UniTask InitializeAsync()
	{
		await AddressableHelper.PreloadSpriteAsync(Address.ItemSprite("RoyalSealNormal"));
		await AddressableHelper.PreloadSpriteAsync(Address.ItemSprite("RoyalSealPremium"));
	}

	public bool HasStock()
	{
		return !(from data in Game.DataManager.GetAllTraitDataList()
			where data.MaxRank > 0
			select data).All((TraitData data) => UpgradeBoard<CardEngravingModel>.PlayerData.GetCardRank(data.ID) >= data.MaxRank);
	}

	protected override void UpdateUI()
	{
		CardData cardData = currentModel.CardData;
		string text = Game.DataManager.GetTraitData(cardData.ID).Name;
		nameText.SetData(LocaleKey.ItemName(text));
		SetCoreStoneIconAsync(text).Forget();
		for (int i = 0; i < engravingRankBoards.Count; i++)
		{
			CardEngravingRankBoard cardEngravingRankBoard = engravingRankBoards[i];
			int myRank = i + 1;
			cardEngravingRankBoard.SetData(cardData, myRank);
		}
		string materialNameOf = GetMaterialNameOf(cardData.Rank);
		ingredientBoard.SetData(materialNameOf, 1, materialNameOf.Equals(lastMaterialName));
		lastMaterialName = materialNameOf;
		UpgradeCheckResult canProceedUpgrade = CanProceedUpgrade();
		UpdateFeedbackText(canProceedUpgrade, onProceed: false);
	}

	public override UpgradeCheckResult CanProceedUpgrade()
	{
		CardData cardData = currentModel.CardData;
		if (cardData.Rank == 3)
		{
			return UpgradeCheckResult.MaximumUpgrade;
		}
		string materialNameOf = GetMaterialNameOf(cardData.Rank);
		UpgradeBoard<CardEngravingModel>.PlayerData.HasEverObtainedItem(materialNameOf, out var current);
		if (current < 1)
		{
			return UpgradeCheckResult.NotEnoughMaterial;
		}
		int upgradeCostOf = GetUpgradeCostOf(cardData.Rank);
		if (UpgradeBoard<CardEngravingModel>.PlayerData.Money >= upgradeCostOf)
		{
			return UpgradeCheckResult.Success;
		}
		return UpgradeCheckResult.NotEnoughOhn;
	}

	public override void OnProceedUpgrade()
	{
		CardData cardData = currentModel.CardData;
		int cardID = currentModel.CardID;
		int rank = cardData.Rank;
		string materialNameOf = GetMaterialNameOf(cardData.Rank);
		int upgradeCostOf = GetUpgradeCostOf(rank);
		UpgradeBoard<CardEngravingModel>.PlayerStat.AddMoney(-upgradeCostOf);
		UpgradeBoard<CardEngravingModel>.PlayerData.TryRemoveAcquiredItem(materialNameOf);
		UpgradeBoard<CardEngravingModel>.PlayerData.SetCardRank(cardID, rank + 1);
	}

	public override void OnBlockUpgrade(UpgradeCheckResult ucResult)
	{
		UpdateFeedbackText(ucResult, onProceed: true);
	}

	protected override void ShowRequirements(bool value)
	{
		ingredientBoard.gameObject.SetActive(value);
		maximumUpgradeText.gameObject.SetActive(!value);
	}

	private int GetUpgradeCostOf(int rank)
	{
		return 0;
	}

	private string GetMaterialNameOf(int rank)
	{
		if (rank != 1)
		{
			return "RoyalSealPremium";
		}
		return "RoyalSealNormal";
	}

	private async UniTask SetCoreStoneIconAsync(string coreStoneName)
	{
		iconImage.DOKill();
		iconImage.SetAlpha(0f);
		AsyncOperationHandle<Sprite> task = Addressables.LoadAssetAsync<Sprite>(Address.TraitSprite(coreStoneName));
		await task;
		iconImage.sprite = task.Result;
		iconImage.DOAlpha(1f, 0.1f);
	}
}
