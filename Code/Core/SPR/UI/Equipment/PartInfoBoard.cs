using System.Linq;
using Coffee.UIExtensions;
using Cysharp.Threading.Tasks;
using Library;
using SPR.L10N;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Equipment;

public class PartInfoBoard : MonoBehaviour
{
	[SerializeField]
	private Image image;

	[SerializeField]
	private LocaleText partTypeText;

	[SerializeField]
	private LocaleText partNameText;

	[SerializeField]
	private LocaleText levelLocaleText;

	[SerializeField]
	private LocaleText descLocaleText;

	[SerializeField]
	private LocaleText descSummaryLocaleText;

	[SerializeField]
	private UIParticle equipParticle;

	[SerializeField]
	private ScrollRect scrollRect;

	private RectTransform rect;

	private CanvasGroup canvasGroup;

	private RectTransform scrollRectTransform;

	private RectTransform summaryRectTransform;

	private const float DefaultPadding = 17f;

	public string PartID { get; private set; }

	private RectTransform Rect => rect ?? (rect = GetComponent<RectTransform>());

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private void Awake()
	{
		scrollRectTransform = scrollRect.GetComponent<RectTransform>();
		summaryRectTransform = descSummaryLocaleText.GetComponent<RectTransform>();
	}

	public void Initialize()
	{
		base.gameObject.SetActive(value: true);
		image.enabled = false;
		partNameText.ClearText();
		descLocaleText.ClearText();
		descSummaryLocaleText.ClearText();
	}

	public void SetData(string partID, PartType partType, Sprite imageSprite)
	{
		PartID = partID;
		base.gameObject.SetActive(value: true);
		image.enabled = true;
		image.sprite = imageSprite;
		if (partID == null)
		{
			image.SetAlpha(4f / 51f);
			partTypeText.ClearText();
			partNameText.ClearText();
			levelLocaleText.ClearText();
			descLocaleText.ClearText();
			descSummaryLocaleText.ClearText();
		}
		else
		{
			image.SetAlpha(1f);
			partTypeText.SetData(LocaleKey.PartCategory(partType));
			partNameText.SetData(LocaleKey.ItemName(partID));
			int partLevel = Game.Instance.SaveData.PlayerData.GetPartLevel(partID);
			int maxLevel = Game.DataManager.GetPartData(partID).MaxLevel;
			levelLocaleText.SetDataFormat("UI_Part_Level", partLevel.ToString(), maxLevel.ToString());
			levelLocaleText.SetColor(Color.white);
			descLocaleText.SetData(LocaleKey.ItemDesc(partID));
			descSummaryLocaleText.SetData(LocaleKey.ItemEffect(partID));
		}
		RebuildLayoutAsync().Forget();
	}

	public void PlayParticle()
	{
		equipParticle.Play();
		RebuildLayoutAsync().Forget();
	}

	public void Scroll(float delta)
	{
		float y = (0f - delta) * scrollRect.scrollSensitivity;
		scrollRect.content.anchoredPosition += new Vector2(0f, y);
	}

	public bool IsScrollAvailable()
	{
		float height = scrollRect.transform.CastObject<RectTransform>().rect.height;
		float num = scrollRect.content.GetComponentsInChildren<TextMeshProUGUI>().Sum((TextMeshProUGUI t) => t.preferredHeight);
		return height <= num;
	}

	private async UniTask RebuildLayoutAsync()
	{
		LayoutRebuilder.ForceRebuildLayoutImmediate(Rect);
		await UniTask.Yield();
		float height = summaryRectTransform.rect.height;
		float num = summaryRectTransform.anchoredPosition.y - height - 17f;
		Vector2 anchoredPosition = scrollRectTransform.anchoredPosition;
		anchoredPosition.y = num;
		scrollRectTransform.anchoredPosition = anchoredPosition;
		Vector2 sizeDelta = scrollRectTransform.sizeDelta;
		sizeDelta.y = Rect.rect.height + num;
		scrollRectTransform.sizeDelta = sizeDelta;
		LayoutRebuilder.ForceRebuildLayoutImmediate(Rect);
		RectTransform[] componentsInChildren = scrollRect.content.GetComponentsInChildren<RectTransform>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(componentsInChildren[i]);
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
		LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRectTransform);
		scrollRect.verticalScrollbar.value = 1f;
		await UniTask.Yield();
	}
}
