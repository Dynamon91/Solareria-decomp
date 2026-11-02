using System.Collections.Generic;
using Coffee.UIExtensions;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.ObjectPool;
using SPR.Save;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace SPR.UI.Inventory;

public class InventoryItemCell : MonoBehaviour, IPoolableObject
{
	[SerializeField]
	private Image itemImage;

	[SerializeField]
	private Image cursorImage;

	[SerializeField]
	private Image cookableIngredientIndicator;

	[SerializeField]
	private TextMeshProUGUI countText;

	[SerializeField]
	private GameObject notiIcon;

	[SerializeField]
	private UIParticle particle;

	private RectTransform rectTransform;

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	public RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	public string ItemID { get; private set; }

	public string PrefabKey => "Inventory Cell.prefab";

	public GameObject GameObject => base.gameObject;

	public void SetData(string id, int quantity, List<string> acquiredItemIDList)
	{
		ItemID = id;
		SetQuantity(quantity);
		SetImageAsync(id).Forget();
		SetIngredientIndicator(acquiredItemIDList, id);
		SetNotiIconActive();
	}

	public void Focus(bool value)
	{
		if (value)
		{
			WanderingData.WatchInventoryItem(ItemID);
		}
		SetNotiIconActive();
		cursorImage.gameObject.SetActive(value);
	}

	public async UniTask ShakeImageAsync()
	{
		await Util.DoShakeForSecond(itemImage.rectTransform).WaitAsync();
		particle.Play();
		await UniTask.WaitForSeconds(0.2f);
		await itemImage.DOAlpha(0f, 0.15f).WaitAsync();
		await UniTask.WaitForSeconds(0.15f);
	}

	private async UniTask SetImageAsync(string id)
	{
		itemImage.DOKill();
		if (id.IsNullOrEmpty())
		{
			itemImage.sprite = null;
			itemImage.color = new Color(1f, 1f, 1f, 0f);
			itemImage.gameObject.SetActive(value: false);
			return;
		}
		itemImage.color = new Color(1f, 1f, 1f, 0f);
		itemImage.gameObject.SetActive(value: true);
		AsyncOperationHandle<Sprite> task = Addressables.LoadAssetAsync<Sprite>(Address.ItemSprite(id));
		await task;
		Sprite result = task.Result;
		itemImage.sprite = result;
		itemImage.DOFade(1f, 0.25f);
	}

	private void SetIngredientIndicator(List<string> acquiredItemIDList, string id)
	{
		ItemData itemData = Game.DataManager.GetItemData(id);
		if (itemData == null || itemData.ItemType != ItemType.Ingredient)
		{
			cookableIngredientIndicator.gameObject.SetActive(value: false);
			return;
		}
		bool active = Game.DataManager.IsCookableIngredient(acquiredItemIDList, id);
		cookableIngredientIndicator.gameObject.SetActive(active);
	}

	private void SetNotiIconActive()
	{
		bool notiIconActive = WanderingData.IsUnwatchedInventoryItem(ItemID);
		SetNotiIconActive(notiIconActive);
	}

	private void SetNotiIconActive(bool value)
	{
		notiIcon.SetActive(value);
	}

	private void SetQuantity(int quantity)
	{
		if (quantity <= 1)
		{
			countText.transform.parent.gameObject.SetActive(value: false);
			countText.text = string.Empty;
		}
		else
		{
			countText.transform.parent.gameObject.SetActive(value: true);
			countText.text = quantity.ToString();
		}
	}

	public void InitializeBeforeUse()
	{
		ItemID = null;
		SetImageAsync(null).Forget();
		SetQuantity(0);
		SetNotiIconActive(value: false);
		base.gameObject.SetActive(value: true);
		cursorImage.gameObject.SetActive(value: false);
	}

	public void ReturnToPool()
	{
		base.gameObject.SetActive(value: false);
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
