namespace SPR.UI.Store;

public class ItemGridCell : ItemCell
{
	public override string PrefabKey => "Item Grid Cell.prefab";

	protected override void SetName(string itemID)
	{
	}

	protected override void SetCount(int quantity)
	{
		bool flag = quantity > 1;
		itemCountText.transform.parent.gameObject.SetActive(flag);
		itemCountText.text = (flag ? quantity.ToString() : string.Empty);
	}
}
