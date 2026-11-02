using System.Collections.Generic;
using Library;
using SPR.L10N;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Store;

public class StoreItemInfoBoard : ItemInfoBoard
{
	[Header("Store Item Info")]
	[SerializeField]
	private IngredientBoard ingredientBoard;

	[SerializeField]
	private LocaleText flavorLocaleText;

	[SerializeField]
	private GameObject priceObject;

	[SerializeField]
	private Image priceImage;

	[SerializeField]
	private TextMeshProUGUI priceText;

	[SerializeField]
	private LocaleText buyKeyGuideText;

	private static StoreType StoreType => Game.Instance.UIManager.FieldCanvas.StorePanel.CurrentStoreType;

	private static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	private bool NeedIngredient
	{
		get
		{
			StoreType storeType = StoreType;
			return storeType == StoreType.Restaurant || storeType == StoreType.TouristPot;
		}
	}

	public void ClearBoard()
	{
		SetData("");
		SetMoneyActive(active: false);
		ingredientBoard.Clear();
	}

	public void SetData(StoreItemData itemData, ActorName seller)
	{
		scrollRect.verticalNormalizedPosition = 1f;
		if (itemData == null)
		{
			DebugLog.GeneralLogError("상점에 아이템이 없는데 상점을 열었습니다!");
			return;
		}
		SetData(itemData.ID);
		SetMoney(itemData);
		string flavorLocaleKey = GetFlavorLocaleKey(itemData, seller);
		if (flavorLocaleKey.IsNullOrEmpty())
		{
			flavorLocaleText.gameObject.SetActive(value: false);
		}
		else
		{
			flavorLocaleText.gameObject.SetActive(value: true);
			flavorLocaleText.SetData(flavorLocaleKey);
		}
		AdjustScrollRectSize(NeedIngredient);
		if (NeedIngredient)
		{
			Dictionary<string, int> ingredientData = itemData.GetIngredientData();
			ingredientBoard.SetData(ingredientData);
		}
		else
		{
			ingredientBoard.SetData(null);
		}
		if (seller == ActorName.TouristPot)
		{
			SetMoneyActive(active: false);
		}
		buyKeyGuideText.SetData((seller == ActorName.TouristPot) ? "UI_KeyGuide_Cook" : "UI_KeyGuide_Buy");
	}

	protected override void SetLayout(string id)
	{
	}

	public override void Scroll(float delta)
	{
		float verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + delta * scrollRect.scrollSensitivity * Time.deltaTime);
		scrollRect.verticalNormalizedPosition = verticalNormalizedPosition;
	}

	protected override string GetItemDescLocaleKey(string itemId)
	{
		return LocaleKey.ItemDescStore(itemId);
	}

	private void SetMoney(StoreItemData itemData)
	{
		SetMoneyActive(active: true);
		priceText.text = itemData.Price.ToString();
		priceText.color = ((PlayerStat.Money >= itemData.Price) ? Color.white : new Color(1f, 0.5f, 0.5f, 1f));
	}

	private void SetMoneyActive(bool active)
	{
		priceObject.SetActive(active);
	}

	private void AdjustScrollRectSize(bool needIngredient)
	{
	}

	private static string GetFlavorLocaleKey(StoreItemData itemData, ActorName seller)
	{
		if (seller != ActorName.Jima && seller != ActorName.Muha)
		{
			return null;
		}
		string text = LocaleKey.ItemFlavorText(itemData.ID, seller);
		if (itemData.ConditionType == ConditionType.Description)
		{
			text += (Trigger.CheckTrigger(itemData.Condition) ? "_Already" : "");
		}
		return text;
	}

	protected override void SetActiveUseKeyGuide(ItemType itemType)
	{
	}
}
