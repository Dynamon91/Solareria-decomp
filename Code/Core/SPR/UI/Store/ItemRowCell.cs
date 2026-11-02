using SPR.L10N;
using UnityEngine;

namespace SPR.UI.Store;

public class ItemRowCell : ItemCell
{
	[SerializeField]
	private LocaleText itemNameLocaleText;

	public override string PrefabKey => "Item Row Cell.prefab";

	protected override void SetName(string itemID)
	{
		itemNameLocaleText.SetData(LocaleKey.ItemName(itemID));
	}

	protected override void SetCount(int quantity)
	{
		itemCountText.text = quantity.ToString();
	}
}
