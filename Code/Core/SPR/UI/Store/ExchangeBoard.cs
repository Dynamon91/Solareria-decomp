using System;
using System.Collections.Generic;
using SPR.L10N;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Store;

[Serializable]
public class ExchangeBoard
{
	[SerializeField]
	private Animator animator;

	[SerializeField]
	private Image itemImage;

	[SerializeField]
	private LocaleText nameLocaleText;

	[SerializeField]
	private TextMeshProUGUI quantityText;

	[SerializeField]
	private GameObject quantityLeftArrow;

	[SerializeField]
	private GameObject quantityRightArrow;

	[SerializeField]
	private TextMeshProUGUI priceText;

	private StoreItemData currentItemData;

	private static readonly int Increase = Animator.StringToHash("Increase");

	private static readonly int Decrease = Animator.StringToHash("Decrease");

	public int Quantity { get; private set; }

	public int TotalPrice { get; private set; }

	public void SetDataAndActivate(StoreItemData itemData, Dictionary<string, Sprite> cachedSprite)
	{
		itemImage.enabled = true;
		itemImage.sprite = cachedSprite[itemData.ID];
		nameLocaleText.SetData(LocaleKey.ItemName(itemData.ID));
		currentItemData = itemData;
		SetQuantity(1, increase: false, initialize: true);
	}

	public void Deactivate()
	{
		currentItemData = null;
		itemImage.sprite = null;
		itemImage.enabled = false;
		nameLocaleText.SetData(string.Empty);
		SetQuantity(1, increase: false, initialize: true);
	}

	public void SetQuantity(int value, bool increase = false, bool initialize = false)
	{
		int num = currentItemData?.Price ?? 0;
		int num2 = currentItemData?.Quantity ?? 1;
		Quantity = Mathf.Clamp(value, 1, num2);
		TotalPrice = Quantity * num;
		quantityText.text = Quantity.ToString();
		priceText.text = (num * Quantity).ToString();
		if (!initialize)
		{
			if ((quantityRightArrow.activeSelf && increase) || (quantityLeftArrow.activeSelf && !increase))
			{
				Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
			}
			else
			{
				Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Common_Negative");
			}
		}
		quantityLeftArrow.SetActive(Quantity > 1);
		quantityRightArrow.SetActive(Quantity < num2);
		animator.SetTrigger(increase ? Increase : Decrease);
	}
}
