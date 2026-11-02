using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Store;

public class ItemListBoard : MonoBehaviour
{
	[SerializeField]
	private RectTransform itemBoardParent;

	[SerializeField]
	private RectTransform itemMarker;

	[SerializeField]
	private Image arrowUpImage;

	[SerializeField]
	private Image arrowDownImage;

	private readonly List<StoreItemBoard> currentItemBoards = new List<StoreItemBoard>();

	private int currentIndex;

	private int currentBoardCenter;

	private int currentMarkerRelativePosition;

	private const float TweenDuration = 0.4f;

	public StoreItemBoard SelectedItem
	{
		get
		{
			if (currentItemBoards.Count <= 0)
			{
				return null;
			}
			return currentItemBoards[currentIndex];
		}
	}

	public StoreItemData SelectedItemData
	{
		get
		{
			if (currentItemBoards.Count <= 0)
			{
				return null;
			}
			return currentItemBoards[currentIndex].Item;
		}
	}

	public void SetItemList(IReadOnlyList<StoreItemData> storeItemDatas, bool reset)
	{
		ClearItemBoards();
		foreach (StoreItemData storeItemData in storeItemDatas)
		{
			StoreItemBoard storeItemBoard = Game.Instance.ObjectPoolManager.GetObject<StoreItemBoard>("Store Item Board.prefab", itemBoardParent);
			storeItemBoard.transform.localScale = Vector3.one;
			Sprite sprite_Unsafe = AddressableHelper.GetSprite_Unsafe(Address.ItemSprite(storeItemData.ID));
			storeItemBoard.SetData(storeItemData, sprite_Unsafe);
			currentItemBoards.Add(storeItemBoard);
		}
		currentIndex = ((!reset) ? Mathf.Clamp(currentIndex, 0, storeItemDatas.Count - 1) : 0);
		currentBoardCenter = (reset ? 1 : Mathf.Clamp(currentBoardCenter, 0, storeItemDatas.Count - 1));
		currentMarkerRelativePosition = currentIndex - currentBoardCenter;
		itemMarker.gameObject.SetActive(value: true);
		MoveItemBoard(isInstant: true).Forget();
	}

	public bool CanMove(bool moveUp)
	{
		int num = currentIndex + ((!moveUp) ? 1 : (-1));
		if (num >= 0)
		{
			return num < currentItemBoards.Count;
		}
		return false;
	}

	public void Move(bool moveUp)
	{
		currentIndex += ((!moveUp) ? 1 : (-1));
		int num = currentIndex - currentBoardCenter;
		if (num <= 1)
		{
			if (num < -1)
			{
				currentBoardCenter--;
			}
		}
		else
		{
			currentBoardCenter++;
		}
		currentMarkerRelativePosition = currentIndex - currentBoardCenter;
		MoveItemBoard(isInstant: false).Forget();
	}

	private async UniTask MoveItemBoard(bool isInstant)
	{
		Sequence s = DOTween.Sequence();
		itemBoardParent.DOKill();
		itemMarker.DOKill();
		arrowUpImage.DOKill();
		arrowDownImage.DOKill();
		int num = ((currentBoardCenter > 1) ? 1 : 0);
		int num2 = ((currentBoardCenter < currentItemBoards.Count - 2) ? 1 : 0);
		float duration = (isInstant ? 0f : 0.4f);
		await s.Join(itemBoardParent.DOAnchorPosY(currentBoardCenter * 100, duration)).Join(itemMarker.DOAnchorPosY(currentMarkerRelativePosition * -100, duration)).Join(arrowUpImage.DOAlpha(num, duration))
			.Join(arrowDownImage.DOAlpha(num2, duration))
			.WaitAsync();
	}

	public void ClearItemBoards()
	{
		currentItemBoards.ForEach(delegate(StoreItemBoard ib)
		{
			ib.ReturnToPool();
		});
		currentItemBoards.Clear();
		itemMarker.gameObject.SetActive(value: false);
	}
}
