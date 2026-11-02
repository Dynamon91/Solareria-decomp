using Library;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Memory;

public class MemoryDetailBoard : MonoBehaviour
{
	[SerializeField]
	private LocaleText nameText;

	[SerializeField]
	private LocaleText subtitleText;

	[SerializeField]
	private Image portraitImage;

	[SerializeField]
	private RectTransform descriptionParent;

	[SerializeField]
	private LocaleText knowText;

	[SerializeField]
	private LocaleText clearText;

	[SerializeField]
	private LocaleText truthText;

	[SerializeField]
	private GameObject updateLineObject;

	[SerializeField]
	private GameObject updateLineObjectTruth;

	private RectTransform rect;

	private GameObject PortraitImagePlaceholder => portraitImage.transform.parent.gameObject;

	public void SetData(MemoryData data)
	{
		if ((object)rect == null)
		{
			rect = GetComponent<RectTransform>();
		}
		portraitImage.sprite = null;
		portraitImage.color = Color.clear;
		PortraitImagePlaceholder.SetActive(value: true);
		if (data.IsUnknown)
		{
			nameText.SetData("Name_Unknown");
			nameText.SetAlpha(0.4f);
			subtitleText.ClearText();
			knowText.ClearText();
			clearText.ClearText();
			truthText.ClearText();
			updateLineObject.SetActive(value: false);
			updateLineObjectTruth.SetActive(value: false);
			return;
		}
		MemoryCategory category = data.Category;
		SetImage(category, data);
		nameText.SetData(MenuMemoryPanel.GetTitleID(data));
		nameText.SetAlpha(1f);
		if (category == MemoryCategory.Boss)
		{
			subtitleText.SetData($"Memory_{category}_{data.ID}_Subtitle");
		}
		else
		{
			subtitleText.ClearText();
		}
		string data2 = ((category == MemoryCategory.Record) ? (data.ID + "_Desc") : $"Memory_{category}_{data.ID}_Know");
		knowText.SetData(data2);
		string text = GetClearId(data, category);
		if ((category == MemoryCategory.Boss || category == MemoryCategory.Character) && data.Level == MemoryLevel.Clear)
		{
			if (data.ID.Equals("Chasa"))
			{
				text = "Memory_Boss_Chasa_Clear";
			}
			clearText.SetData(text);
		}
		else
		{
			clearText.ClearText();
		}
		bool flag = (category == MemoryCategory.Boss || category == MemoryCategory.Character) && data.ID.Equals(BossName.Chasa.ToString()) && data.Level == MemoryLevel.Clear;
		if (flag && Game.Instance.SaveData.TalkData.GetTalkCount("ChasaRiverCutscene_2_1") > 0)
		{
			truthText.SetData("Memory_Chasa_Clear_Truth");
		}
		else
		{
			truthText.ClearText();
		}
		bool active = (category == MemoryCategory.Boss || category == MemoryCategory.Character) && data.Level != MemoryLevel.None && DataManager.Instance.IsLocaleExist(text);
		updateLineObject.SetActive(active);
		updateLineObjectTruth.SetActive(flag);
		LayoutRebuilder.ForceRebuildLayoutImmediate(descriptionParent);
	}

	private static string GetClearId(MemoryData data, MemoryCategory category)
	{
		string arg = string.Empty;
		if (data.ID.IsOneOf("Baran", "Myoso") && !Trigger.CheckTrigger("Trigger_MyosoReunionedWithBaran"))
		{
			arg = "_WithoutReunion";
		}
		return $"Memory_{category}_{data.ID}_Clear{arg}";
	}

	private void SetImage(MemoryCategory category, MemoryData data)
	{
		string arg = ((category == MemoryCategory.Boss) ? $"_{data.Level}" : string.Empty);
		if (AddressableHelper.TryGetAsset<Sprite>(MenuMemoryPanel.GetLabel(category), $"{category}_{data.ID}{arg}", out var asset))
		{
			PortraitImagePlaceholder.SetActive(value: true);
			portraitImage.sprite = asset;
			portraitImage.color = Color.white;
		}
		else
		{
			PortraitImagePlaceholder.SetActive(value: false);
			portraitImage.sprite = null;
		}
	}
}
