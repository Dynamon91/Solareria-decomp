using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using Rewired;
using SPR.L10N;
using SPR.Library;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Store;

public class ItemInfoBoard : MonoBehaviour, IControllerEventUpdate
{
	[SerializeField]
	private LocaleText nameText;

	[SerializeField]
	private LocaleText categoryText;

	[SerializeField]
	protected LocaleText descText;

	[SerializeField]
	protected LocaleText summaryDescText;

	[SerializeField]
	private Image itemImage;

	[SerializeField]
	protected ScrollRect scrollRect;

	[Header("Key Guides")]
	[SerializeField]
	private GameObject[] scrollKeyGuides;

	[SerializeField]
	private GameObject eatKeyGuide;

	private const int ShortHeight = 300;

	private const int LongHeight = 585;

	private RectTransform scrollRectTransform;

	private GameObject ImagePlaceholder => itemImage.transform.parent.gameObject;

	private RectTransform ScrollRectTransform => scrollRectTransform ?? (scrollRectTransform = scrollRect.GetComponent<RectTransform>());

	private void Awake()
	{
		RewiredHelper.RegisterControllerChangeEvent(this);
	}

	public void OnControllerChanged(ControllerStatusChangedEventArgs args)
	{
	}

	public void SetData(string itemId)
	{
		scrollRect.verticalNormalizedPosition = 1f;
		SetLayout(itemId);
		SetImageAsync(itemId).Forget();
		SetDataWithoutImage(itemId);
	}

	public virtual void Scroll(float delta)
	{
		if (!scrollRect.IsNull())
		{
			float y = (0f - delta) * scrollRect.scrollSensitivity;
			scrollRect.content.anchoredPosition += new Vector2(0f, y);
		}
	}

	private void SetDataWithoutImage(string itemId)
	{
		bool flag = !itemId.IsNullOrEmpty();
		base.gameObject.SetActive(flag);
		if (flag)
		{
			ItemType itemType = Game.DataManager.GetItemData(itemId).ItemType;
			categoryText.SetData((itemType == ItemType.Part) ? LocaleKey.PartCategory(Game.DataManager.GetPartData(itemId).PartType) : LocaleKey.ItemCategory(itemType));
			summaryDescText.SetData(LocaleKey.ItemEffect(itemId));
			nameText.SetData(LocaleKey.ItemName(itemId));
			if (itemId == "CookingLog")
			{
				descText.SetRawText(GetFoodLog());
			}
			else
			{
				descText.SetData(GetItemDescLocaleKey(itemId));
			}
			summaryDescText.gameObject.SetActive(!summaryDescText.GetCurrentText().IsNullOrEmpty());
			UIHelper.RebuildLayoutAfterFramesAsync(scrollRect.content, 1, SetActiveScrollKeyGuide).Forget();
			SetActiveUseKeyGuide(itemType);
		}
	}

	protected virtual string GetItemDescLocaleKey(string itemId)
	{
		return LocaleKey.ItemDesc(itemId);
	}

	protected virtual void SetLayout(string id)
	{
		bool flag;
		if (id.IsNullOrEmpty())
		{
			flag = false;
		}
		else
		{
			ItemType itemType = Game.DataManager.GetItemData(id).ItemType;
			flag = itemType == ItemType.Note || itemType == ItemType.LongNote;
		}
		ImagePlaceholder.SetActive(!flag);
		Vector2 sizeDelta = ScrollRectTransform.sizeDelta;
		ScrollRectTransform.sizeDelta = new Vector2(sizeDelta.x, flag ? 585 : 300);
	}

	private async UniTask SetImageAsync(string id)
	{
		itemImage.DOKill();
		itemImage.color = new Color(1f, 1f, 1f, 0f);
		if (!id.IsNullOrEmpty())
		{
			Sprite sprite = await AddressableHelper.GetSpriteAsync(Address.ItemSprite(id));
			itemImage.sprite = sprite;
			itemImage.DOFade(1f, 0.25f);
		}
	}

	private static string GetFoodLog()
	{
		StringBuilder stringBuilder = new StringBuilder();
		IReadOnlyList<string> appliedFoodIDList = Game.Instance.SaveData.PlayerData.AppliedFoodIDList;
		List<string> list = new List<string>();
		foreach (string item in appliedFoodIDList)
		{
			if (Game.DataManager.GetItemData(item).ItemType == ItemType.Food)
			{
				list.Add(item);
			}
		}
		foreach (string item2 in list)
		{
			stringBuilder.Append(" · " + Locale.GetLocaleString(LocaleKey.ItemName(item2)) + " [" + Locale.GetLocaleString(LocaleKey.ItemEffect(item2)) + "]\n");
		}
		return stringBuilder.ToString();
	}

	private void SetActiveScrollKeyGuide()
	{
		float height = scrollRect.transform.CastObject<RectTransform>().rect.height;
		float num = scrollRect.content.GetComponentsInChildren<TextMeshProUGUI>().Sum((TextMeshProUGUI t) => t.preferredHeight);
		GameObject[] array = scrollKeyGuides;
		for (int num2 = 0; num2 < array.Length; num2++)
		{
			array[num2].SetActive(height <= num);
		}
	}

	protected virtual void SetActiveUseKeyGuide(ItemType itemType)
	{
		if (itemType == ItemType.Food)
		{
			eatKeyGuide.SetActive(value: true);
		}
		else
		{
			eatKeyGuide.SetActive(value: false);
		}
	}
}
