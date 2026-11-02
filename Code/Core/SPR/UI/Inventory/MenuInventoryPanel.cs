using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using SPR.Save;
using SPR.UI.Menu;
using SPR.UI.Store;
using UnityEngine;

namespace SPR.UI.Inventory;

public class MenuInventoryPanel : MenuPanel
{
	[Header("상단 카테고리")]
	[SerializeField]
	private CategoryList categoryList;

	[Header("좌측 아이템 그리드")]
	[SerializeField]
	private ScrollBoard scrollBoard;

	[SerializeField]
	private RectTransform contentGroup;

	[SerializeField]
	private List<InventorySlotGroup> slotGroups;

	[SerializeField]
	private int columnSize;

	[Header("우측 아이템 상세 정보 보드")]
	[SerializeField]
	private ItemInfoBoard itemInfoBoard;

	[Header("팝업")]
	[SerializeField]
	private InventoryPopup popup;

	private readonly List<InventoryItemCell> allCells = new List<InventoryItemCell>();

	private int selectedCellIndex = -1;

	private InventoryCategory currentCategory;

	private bool canEat;

	public override MenuCategory Category => MenuCategory.Inventory;

	protected override bool CanChangeCategory => !MenuPanel.IsRest;

	private bool IsFocusingCategory => selectedCellIndex == -1;

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	public override void Initialize()
	{
		categoryList.Initialize();
		popup.Initialize();
	}

	protected override void OnActivate()
	{
		canEat = false;
		FocusCategory(currentCategory, shouldFocusCategory: false, force: true);
	}

	protected override void OnControl()
	{
		int subCategory = MenuPanel.InputManager.subCategory;
		int horizontalDownUI = MenuPanel.InputManager.horizontalDownUI;
		int verticalDownUI = MenuPanel.InputManager.verticalDownUI;
		bool confirmDown = MenuPanel.InputManager.confirmDown;
		bool flag = MenuPanel.InputManager.menuDown || MenuPanel.InputManager.cancelDown;
		int num = (IsFocusingCategory ? Util.GetPrioritizedControl(subCategory, horizontalDownUI) : Util.GetNotOverlappedControl(subCategory, horizontalDownUI));
		if (num != 0)
		{
			InventoryCategory newCategory = Util.AddToEnumCircular(currentCategory, num);
			FocusCategory(newCategory, subCategory == 0);
		}
		else if ((horizontalDownUI == 0 && verticalDownUI == 0) || MoveCell(horizontalDownUI, verticalDownUI))
		{
			if (MenuPanel.InputManager.scroll != 0f)
			{
				itemInfoBoard.Scroll(MenuPanel.InputManager.scroll.GetSign());
			}
			if (canEat && confirmDown)
			{
				MenuPanel.InputManager.BlockControlForTask(UseFoodItemAsync(allCells[selectedCellIndex]), "Eat Food Item").Forget();
			}
			if (flag && !IsFocusingCategory)
			{
				SelectCell(-1);
			}
		}
		bool MoveCell(int h, int v)
		{
			int num2 = allCells.CalculateTargetCellIndex(selectedCellIndex, columnSize, h, v, v == 1);
			if (num2 == selectedCellIndex)
			{
				return false;
			}
			SelectCell(num2);
			return true;
		}
		void SelectCell(int targetCellIndex)
		{
			selectedCellIndex = targetCellIndex;
			InventoryItemCell elementSafely = allCells.GetElementSafely(selectedCellIndex);
			FocusCell(elementSafely);
			MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
		}
	}

	protected override void OnDeactivate()
	{
		foreach (InventorySlotGroup slotGroup in slotGroups)
		{
			slotGroup.OnDeactivate();
		}
	}

	private void FocusCategory(InventoryCategory newCategory, bool shouldFocusCategory = false, bool force = false)
	{
		if (force || currentCategory != newCategory)
		{
			currentCategory = newCategory;
			categoryList.Focus((int)newCategory, !force);
			slotGroups.ForEach(delegate(InventorySlotGroup group)
			{
				group.gameObject.SetActive(value: false);
			});
			List<ItemGroup> itemGroupsByCategory = GetItemGroupsByCategory(currentCategory);
			for (int num = 0; num < itemGroupsByCategory.Count; num++)
			{
				slotGroups[num].SetData(itemGroupsByCategory[num], columnSize, itemGroupsByCategory.Count == 1);
			}
			ClearAndSetAllCells();
			InventoryItemCell inventoryItemCell = allCells.FirstOrDefault();
			if (inventoryItemCell.IsNull() || shouldFocusCategory)
			{
				selectedCellIndex = -1;
				FocusCell(null, force);
			}
			else
			{
				selectedCellIndex = 0;
				FocusCell(inventoryItemCell, force);
			}
		}
	}

	private void FocusCell(InventoryItemCell targetCell, bool force = false)
	{
		UpdateCategoryCursor();
		if (targetCell.IsNull())
		{
			DefocusAllCells();
			itemInfoBoard.SetData(null);
			return;
		}
		scrollBoard.ScrollIfTargetIsOutOfViewport(targetCell.RectTransform, targetCell.RectTransform.sizeDelta.y, force);
		DefocusAllCells();
		targetCell.Focus(value: true);
		itemInfoBoard.SetData(targetCell.ItemID);
		canEat = Game.DataManager.GetItemData(targetCell.ItemID).ItemType == ItemType.Food;
	}

	private void DefocusAllCells()
	{
		foreach (InventoryItemCell item in allCells.Where((InventoryItemCell cell) => !cell.IsNull()))
		{
			item.Focus(value: false);
		}
	}

	private void UpdateCategoryCursor()
	{
		int cursor = (int)(IsFocusingCategory ? currentCategory : ((InventoryCategory)(-1)));
		categoryList.SetCursor(cursor);
	}

	private void ClearAndSetAllCells()
	{
		allCells.Clear();
		foreach (InventorySlotGroup item in slotGroups.Where((InventorySlotGroup group) => group.gameObject.activeSelf))
		{
			foreach (InventoryItemCell cell in item.Cells)
			{
				allCells.Add(cell);
			}
		}
		UIHelper.RebuildLayoutAfterFramesAsync(contentGroup).Forget();
	}

	public static List<ItemGroup> GetItemGroupsByCategory(InventoryCategory category)
	{
		return category switch
		{
			InventoryCategory.Collectable => new List<ItemGroup>
			{
				ItemGroup.Collectable,
				ItemGroup.Note,
				ItemGroup.InfoStone
			}, 
			InventoryCategory.Material => new List<ItemGroup>
			{
				ItemGroup.Material,
				ItemGroup.Ingredient
			}, 
			InventoryCategory.Consumable => new List<ItemGroup>
			{
				ItemGroup.Food,
				ItemGroup.Gem,
				ItemGroup.PlagueSprout
			}, 
			InventoryCategory.Special => new List<ItemGroup> { ItemGroup.Special }, 
			_ => throw new ArgumentOutOfRangeException("category", category, null), 
		};
	}

	public static List<ItemType> GetItemTypesByGroup(ItemGroup itemGroup)
	{
		return itemGroup switch
		{
			ItemGroup.Collectable => new List<ItemType>
			{
				ItemType.Collectable,
				ItemType.CookingLog
			}, 
			ItemGroup.Note => new List<ItemType>
			{
				ItemType.Note,
				ItemType.LongNote
			}, 
			ItemGroup.Food => new List<ItemType> { ItemType.Food }, 
			ItemGroup.PlagueSprout => new List<ItemType> { ItemType.PlagueSprout }, 
			ItemGroup.InfoStone => new List<ItemType> { ItemType.InfoStone }, 
			ItemGroup.Material => new List<ItemType> { ItemType.Material }, 
			ItemGroup.Ingredient => new List<ItemType> { ItemType.Ingredient }, 
			ItemGroup.Gem => new List<ItemType> { ItemType.Gem }, 
			ItemGroup.Special => new List<ItemType>
			{
				ItemType.SpiritFlame,
				ItemType.Relic,
				ItemType.Wristband,
				ItemType.GuardianStone
			}, 
			_ => throw new ArgumentOutOfRangeException("itemGroup", itemGroup, null), 
		};
	}

	public static List<ItemType> GetItemTypesByCategory(InventoryCategory category)
	{
		List<ItemGroup> itemGroupsByCategory = GetItemGroupsByCategory(category);
		List<ItemType> list = new List<ItemType>();
		foreach (ItemGroup item in itemGroupsByCategory)
		{
			list.AddRange(GetItemTypesByGroup(item));
		}
		return list;
	}

	private async UniTask UseFoodItemAsync(InventoryItemCell targetCell)
	{
		string foodID = targetCell.ItemID;
		MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_FoodConsume");
		await targetCell.ShakeImageAsync();
		PlayerData.UseFoodItem(foodID);
		PlayerData.TryRemoveAcquiredItem(foodID);
		slotGroups.Find((InventorySlotGroup s) => s.ItemGroup == ItemGroup.Food).SetData(ItemGroup.Food, columnSize, isOnlyOneGroupInCategory: false);
		ClearAndSetAllCells();
		selectedCellIndex = GetNextFoodIndex(selectedCellIndex);
		if (selectedCellIndex < 0)
		{
			FocusCell(null);
		}
		else
		{
			InventoryItemCell targetCell2 = allCells[selectedCellIndex];
			FocusCell(targetCell2);
		}
		popup.Show(foodID);
		if (PlayerData.AppliedFoodIDList.Count((string id) => Game.DataManager.GetItemData(id).ItemType == ItemType.Food) == 1)
		{
			new PureItem("CookingLog").AddToPlayerData();
		}
	}

	private int GetNextFoodIndex(int currentIndex)
	{
		int num = allCells.FindLastNonNullIndex(currentIndex);
		if (num < 0)
		{
			return -1;
		}
		if (Game.DataManager.GetItemData(allCells[num].ItemID).ItemType != ItemType.Food)
		{
			return allCells.FindFirstNonNullIndex(currentIndex);
		}
		return num;
	}
}
