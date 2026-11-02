using System.Text;
using DG.Tweening;
using Library;
using SPR.L10N;
using UnityEngine;

namespace SPR.UI.Inventory;

public class InventoryPopup : MonoBehaviour
{
	[SerializeField]
	private LocaleText localeText;

	private CanvasGroup canvasGroup;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	public void Initialize()
	{
		CanvasGroup.alpha = 0f;
		base.gameObject.SetActive(value: false);
	}

	public void Show(string foodID)
	{
		base.gameObject.SetActive(value: true);
		CanvasGroup.alpha = 0f;
		FoodData foodData = Game.DataManager.GetFoodData(foodID);
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < foodData.StatTypes.Count; i++)
		{
			if (i > 0)
			{
				stringBuilder.AppendLine();
			}
			StatType statType = foodData.StatTypes[i];
			float num = foodData.StatValues[i];
			string localeString = Locale.GetLocaleString(LocaleKey.StatName(statType));
			string localeFormat = Locale.GetLocaleFormat((num > 0f) ? "UI_Increase_Format" : "UI_Decrease_Format", localeString);
			stringBuilder.Append(localeFormat);
		}
		localeText.SetRawText(stringBuilder.ToString());
		DOTween.Sequence().Append(CanvasGroup.DOAlpha(1f, 0.5f)).AppendInterval(1f)
			.Append(CanvasGroup.DOAlpha(0f, 0.5f));
	}
}
