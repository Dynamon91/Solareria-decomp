using System.Collections.Generic;
using System.Linq;
using Library;
using UnityEngine;

namespace SPR.UI.Inventory;

public class InventorySlotGroup : MonoBehaviour
{
	[SerializeField]
	private LocaleText groupTitle;

	[SerializeField]
	private Transform gridTransform;

	public readonly List<InventoryItemCell> Cells = new List<InventoryItemCell>();

	public ItemGroup ItemGroup { get; private set; }

	public void SetData(ItemGroup itemGroup, int columnSize, bool isOnlyOneGroupInCategory)
	{
		ClearChildren();
		ItemGroup = itemGroup;
		List<(string, int)> itemsByGroup = GetItemsByGroup(itemGroup);
		if (itemsByGroup.Count <= 0)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		base.gameObject.SetActive(value: true);
		CreateItemGrid(itemsByGroup, columnSize);
		bool flag = itemsByGroup.Count > 0 && !isOnlyOneGroupInCategory;
		groupTitle.gameObject.SetActive(flag);
		if (flag)
		{
			groupTitle.SetData($"Inventory_{itemGroup}");
		}
	}

	public void OnDeactivate()
	{
		foreach (InventoryItemCell item in Cells.Where((InventoryItemCell cell) => !cell.IsNull()))
		{
			item.ReturnToPool();
		}
		Cells.Clear();
	}

	private void ClearChildren()
	{
		foreach (Transform item in gridTransform.Cast<Transform>().ToList())
		{
			if (item.TryGetComponent<InventoryItemCell>(out var component))
			{
				component.ReturnToPool();
			}
			else
			{
				Object.Destroy(item.gameObject);
			}
		}
		Cells.Clear();
	}

	private List<(string itemID, int quantity)> GetItemsByGroup(ItemGroup itemGroup)
	{
		IReadOnlyList<ItemData> allItemDataList = Game.DataManager.GetAllItemDataList();
		IReadOnlyList<(string, int)> acquiredItems = Game.Instance.SaveData.PlayerData.AcquiredItems;
		List<(string, int)> list = new List<(string, int)>();
		List<ItemType> itemTypesByGroup = MenuInventoryPanel.GetItemTypesByGroup(itemGroup);
		if (itemTypesByGroup.Count == 0)
		{
			return list;
		}
		foreach (ItemData itemData in allItemDataList)
		{
			(string, int) item = acquiredItems.FirstOrDefault<(string, int)>(((string itemID, int quantity) i) => i.itemID == itemData.ID);
			if (!string.IsNullOrEmpty(item.Item1))
			{
				ItemType itemType = itemData.ItemType;
				if (itemTypesByGroup.Contains(itemType))
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	private void CreateItemGrid(List<(string itemID, int quantity)> itemPairList, int columnSize)
	{
		List<string> acquiredItemIDList = itemPairList.Select(((string itemID, int quantity) p) => p.itemID).ToList();
		int num = Mathf.CeilToInt((float)itemPairList.Count / (float)columnSize) * columnSize;
		for (int num2 = 0; num2 < num; num2++)
		{
			InventoryItemCell inventoryItemCell;
			if (num2 < itemPairList.Count)
			{
				(string, int) tuple = itemPairList[num2];
				inventoryItemCell = Game.Instance.ObjectPoolManager.GetObject<InventoryItemCell>("Inventory Cell.prefab", gridTransform);
				inventoryItemCell.transform.localScale = Vector3.one;
				inventoryItemCell.SetData(tuple.Item1, tuple.Item2, acquiredItemIDList);
			}
			else
			{
				inventoryItemCell = null;
			}
			Cells.Add(inventoryItemCell);
		}
	}
}
