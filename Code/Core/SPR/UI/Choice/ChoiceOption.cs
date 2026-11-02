using UnityEngine;

namespace SPR.UI.Choice;

public class ChoiceOption : MonoBehaviour
{
	private LocaleText choiceText;

	private RectTransform rect;

	private GameObject notiIcon;

	private LocaleText ChoiceText => choiceText ?? (choiceText = GetComponent<LocaleText>());

	private GameObject NotiIcon => notiIcon ?? (notiIcon = base.transform.GetChild(0).gameObject);

	public RectTransform Rect => rect ?? (rect = GetComponent<RectTransform>());

	public void Initialize()
	{
		base.gameObject.SetActive(value: false);
		NotiIcon.SetActive(value: false);
	}

	public void SetData(string text)
	{
		ChoiceText.SetRawText(text);
	}

	public void Show(bool showNoti = false)
	{
		base.gameObject.SetActive(value: true);
		NotiIcon.SetActive(showNoti);
	}

	public void Focus(bool value)
	{
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
	}
}
