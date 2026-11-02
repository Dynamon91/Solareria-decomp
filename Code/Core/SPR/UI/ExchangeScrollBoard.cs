using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Library;
using SPR.UI.Store;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class ExchangeScrollBoard : MonoBehaviour
{
	private enum BoardType
	{
		Row,
		Grid
	}

	[SerializeField]
	private BoardType boardType;

	[Tooltip("Board Type이 Row일 경우 RowSize는 무시되고 1로 고정됩니다.")]
	[SerializeField]
	private int rowSize = 1;

	[Header("Scroll")]
	[SerializeField]
	private RectTransform viewport;

	[SerializeField]
	private RectTransform contentGroup;

	private ScrollRect scrollRect;

	private List<ItemCell> allCells;

	private int currentIndex;

	private const float ScrollDuration = 0.15f;

	private ScrollRect ScrollRect => scrollRect ?? (scrollRect = GetComponent<ScrollRect>());

	private string ItemCellAddress
	{
		get
		{
			if (boardType != BoardType.Row)
			{
				return "Item Grid Cell.prefab";
			}
			return "Item Row Cell.prefab";
		}
	}

	private int RowSize
	{
		get
		{
			if (boardType != BoardType.Row)
			{
				return rowSize;
			}
			return 1;
		}
	}

	private static ObjectPoolManager ObjectPoolManager => Game.Instance.ObjectPoolManager;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public ItemCell CurrentCell
	{
		get
		{
			if (currentIndex >= allCells.Count)
			{
				return null;
			}
			return allCells[currentIndex];
		}
	}

	public void SetData(List<StoreItemData> itemDatas, Dictionary<string, Sprite> cachedSprite)
	{
		if (allCells == null)
		{
			allCells = new List<ItemCell>();
		}
		allCells.ForEach(delegate(ItemCell x)
		{
			ObjectPoolManager.Return(x);
		});
		allCells.Clear();
		foreach (StoreItemData itemData in itemDatas)
		{
			ItemCell itemCell = ObjectPoolManager.GetObject<ItemCell>(ItemCellAddress, contentGroup);
			itemCell.transform.localScale = Vector3.one;
			itemCell.SetData(itemData, cachedSprite[itemData.ID]);
			allCells.Add(itemCell);
		}
	}

	public void MoveCursorToFirstCell(Action<ItemCell> onFocusFirstCell, Action failToFocusFirstCell = null)
	{
		ItemCell firstCell = GetFirstCell();
		if (firstCell.IsNull())
		{
			failToFocusFirstCell?.Invoke();
			return;
		}
		FocusFirstCell();
		onFocusFirstCell?.Invoke(firstCell);
	}

	public bool TryMoveCursor(int horizontal, int vertical, out ItemCell targetCell)
	{
		targetCell = null;
		if (allCells == null || allCells.Count == 0)
		{
			return false;
		}
		int value = currentIndex + horizontal - vertical * RowSize;
		value = Mathf.Clamp(value, 0, allCells.Count - 1);
		if (currentIndex == value)
		{
			return false;
		}
		currentIndex = value;
		targetCell = allCells[currentIndex];
		FocusCell(targetCell);
		return true;
	}

	private void FocusFirstCell()
	{
		ItemCell firstCell = GetFirstCell();
		if (!firstCell.IsNull())
		{
			DefocusAllCells();
			firstCell.Focus(value: true);
			ScrollRect.verticalNormalizedPosition = 1f;
		}
	}

	private void FocusCell(ItemCell cell)
	{
		if (!cell.IsNull())
		{
			DefocusAllCells();
			cell.Focus(value: true);
			ScrollIfTargetIsOutOfViewport(cell.Rect);
			SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
		}
	}

	private void DefocusAllCells()
	{
		foreach (ItemCell item in allCells.Where((ItemCell cell) => !cell.IsNull()))
		{
			item.Focus(value: false);
		}
	}

	private void ScrollIfTargetIsOutOfViewport(RectTransform targetRect)
	{
		Vector3[] array = new Vector3[4];
		Vector3[] array2 = new Vector3[4];
		targetRect.GetWorldCorners(array);
		viewport.GetWorldCorners(array2);
		float y = array[1].y;
		float y2 = array[0].y;
		float y3 = array2[1].y;
		float y4 = array2[0].y;
		float height = contentGroup.rect.height;
		float height2 = viewport.rect.height;
		float num = ((y > y3) ? (y - y3) : ((!(y2 < y4)) ? 0f : (y2 - y4)));
		if (num != 0f)
		{
			float num2 = num / (height - height2);
			float value = Mathf.Clamp01(ScrollRect.verticalNormalizedPosition + num2);
			ScrollRect.DOScroll(value, 0.15f).SetEase(Ease.InOutQuad);
		}
	}

	private ItemCell GetFirstCell()
	{
		if (allCells == null || allCells.Count == 0)
		{
			return null;
		}
		currentIndex = 0;
		return allCells[currentIndex];
	}
}
