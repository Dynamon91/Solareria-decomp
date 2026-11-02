using DG.Tweening;
using SPR.ObjectPool;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Store;

public abstract class ItemCell : MonoBehaviour, IPoolableObject
{
	[SerializeField]
	private Image itemImage;

	[SerializeField]
	protected TextMeshProUGUI itemCountText;

	[SerializeField]
	private GameObject cursorImage;

	private bool isInitialized;

	private RectTransform rectTransform;

	public StoreItemData Item { get; private set; }

	public RectTransform Rect => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	public abstract string PrefabKey { get; }

	public GameObject GameObject => base.gameObject;

	public void SetData(StoreItemData item, Sprite sprite)
	{
		Item = item;
		SetImage(sprite);
		SetName(item.ID);
		SetCount(item.Quantity);
	}

	public void ShakeImage()
	{
		DOTween.Kill(itemImage.rectTransform, complete: true);
		itemImage.rectTransform.DOShakePosition(1f, new Vector3(10f, 0f), 10, 90f, snapping: false, fadeOut: true, ShakeRandomnessMode.Harmonic);
	}

	public void Focus(bool value)
	{
		cursorImage.gameObject.SetActive(value);
	}

	private void SetImage(Sprite sprite)
	{
		if (!isInitialized && (object)sprite != null)
		{
			itemImage.DOKill();
			itemImage.color = new Color(1f, 1f, 1f, 0f);
			itemImage.sprite = sprite;
			itemImage.DOFade(1f, 0.25f);
			isInitialized = true;
		}
	}

	protected abstract void SetName(string itemID);

	protected abstract void SetCount(int quantity);

	public void InitializeBeforeUse()
	{
		isInitialized = false;
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Item = null;
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
