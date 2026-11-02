using DG.Tweening;
using SPR.ObjectPool;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Store;

public class StoreItemBoard : MonoBehaviour, IPoolableObject
{
	[SerializeField]
	private Image itemImage;

	[SerializeField]
	private TextMeshProUGUI itemCountText;

	private bool isInitialized;

	public StoreItemData Item { get; private set; }

	public string PrefabKey => "Store Item Board.prefab";

	public GameObject GameObject => base.gameObject;

	public void SetData(StoreItemData item, Sprite sprite = null)
	{
		Item = item;
		SetImage(sprite);
		SetCount(item.Quantity);
	}

	public void ShakeImage()
	{
		DOTween.Kill(itemImage.rectTransform, complete: true);
		itemImage.rectTransform.DOShakePosition(1f, new Vector3(10f, 0f), 10, 90f, snapping: false, fadeOut: true, ShakeRandomnessMode.Harmonic);
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

	private void SetCount(int quantity)
	{
		bool flag = quantity > 1;
		itemCountText.transform.parent.gameObject.SetActive(flag);
		itemCountText.text = (flag ? quantity.ToString() : string.Empty);
	}

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
