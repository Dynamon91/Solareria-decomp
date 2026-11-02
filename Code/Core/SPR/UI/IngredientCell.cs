using Coffee.UIEffects;
using DG.Tweening;
using SPR.Library;
using SPR.ObjectPool;
using SPR.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class IngredientCell : MonoBehaviour, IPoolableObject
{
	[SerializeField]
	private Image image;

	[SerializeField]
	private TextMeshProUGUI text;

	private UIEffect imageEffect;

	private UIEffect ImageEffect => imageEffect ?? (imageEffect = image.GetComponent<UIEffect>());

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	public string PrefabKey => "Ingredient Cell.prefab";

	public GameObject GameObject => base.gameObject;

	public void SetData(string id, int requiredCount, bool instant = false)
	{
		int current;
		bool hasEverObtainedItem = PlayerData.HasEverObtainedItem(id, out current);
		Sprite sprite_Unsafe = AddressableHelper.GetSprite_Unsafe(Address.ItemSprite(id));
		SetImage(sprite_Unsafe, hasEverObtainedItem, instant);
		SetText(current, requiredCount);
		base.transform.localScale = Vector3.one;
	}

	private void SetImage(Sprite sprite, bool hasEverObtainedItem, bool instant = false)
	{
		image.DOKill();
		image.sprite = sprite;
		image.DOFade(1f, instant ? 0f : 0.25f);
		if (hasEverObtainedItem)
		{
			image.color = new Color(1f, 1f, 1f, 0f);
			ImageEffect.enabled = false;
		}
		else
		{
			image.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
			ImageEffect.enabled = true;
		}
	}

	private void SetText(int current, int required)
	{
		text.text = $"{current} / {required}";
		text.color = ((current >= required) ? GraphicHelper.NormalColor : GraphicHelper.BadColor);
	}

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
