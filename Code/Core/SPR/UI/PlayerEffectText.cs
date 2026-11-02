using System.Text;
using DG.Tweening;
using Library;
using SPR.L10N;
using SPR.Library;
using UnityEngine;

namespace SPR.UI;

public class PlayerEffectText : MonoBehaviour
{
	private LocaleText localeText;

	private RectTransform rectTransform;

	private FrameTimer timer;

	private LocaleText LocaleText => localeText ?? (localeText = GetComponent<LocaleText>());

	private RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	public bool IsPending { get; private set; }

	public void Initialize()
	{
		RectTransform.anchoredPosition = Vector3.zero;
		SetActive(value: false);
	}

	public void SetData(int timerFrame, string id)
	{
		IsPending = true;
		StringBuilder stringBuilder = new StringBuilder();
		FoodData foodData = Game.DataManager.GetFoodData(id);
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
		LocaleText.SetRawText(stringBuilder.ToString());
		timer = new FrameTimer(timerFrame);
		SetActive(value: true);
	}

	private void FixedUpdate()
	{
		if (timer != null)
		{
			timer.Tick();
			if (timer.IsDone)
			{
				SetActive(value: false, instant: false);
			}
		}
	}

	public async void SetActive(bool value, bool instant = true)
	{
		if (!value)
		{
			timer = null;
			if (!instant)
			{
				RectTransform.DOAnchorPosY(3f, 0.5f);
				await LocaleText.DOAlpha(0f, 0.5f).WaitAsync();
				Vector2 anchoredPosition = RectTransform.anchoredPosition;
				anchoredPosition.y = 0f;
				RectTransform.anchoredPosition = anchoredPosition;
				IsPending = false;
			}
		}
		else
		{
			LocaleText.SetAlpha(1f);
		}
		base.gameObject.SetActive(value);
	}
}
