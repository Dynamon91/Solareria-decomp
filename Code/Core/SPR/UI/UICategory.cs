using System;
using System.Collections.Generic;
using Library;
using SPR.Library;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SPR.UI;

public class UICategory : MonoBehaviour
{
	[SerializeField]
	private GameObject cursorObject;

	private GameObject indicator;

	private GameObject notiIcon;

	private TextMeshProUGUI text;

	private RectTransform rectTransform;

	private Image raycastTargetImage;

	private EventTrigger eventTrigger;

	private bool isActive;

	private EventTrigger EventTrigger => eventTrigger ?? (eventTrigger = base.gameObject.AddOrGetComponent<EventTrigger>());

	public RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	public void Initialize(Action<UICategory> subscribeAction)
	{
		bool flag = !cursorObject.IsNull();
		text = base.transform.GetChild(flag ? 1 : 0).GetComponent<TextMeshProUGUI>();
		indicator = base.transform.GetChild((!flag) ? 1 : 2).gameObject;
		if (text.transform.childCount != 0)
		{
			notiIcon = text.transform.GetChild(0).gameObject;
			notiIcon.SetActive(value: false);
		}
		InitializeButtonAction();
		subscribeAction?.Invoke(this);
	}

	public void SetCategoryActive(bool value)
	{
		isActive = value;
		text.SetAlpha(value ? 0.4f : 0.2f);
		indicator.SetActive(value);
	}

	public void SetFocus(bool value)
	{
		float num = (value ? 1f : (isActive ? 0.4f : 0.2f));
		text.SetAlpha(value ? 1f : num);
		indicator.SetActive(value);
	}

	public void SetNotiIcon(bool value)
	{
		if (!notiIcon.IsNull())
		{
			notiIcon.SetActive(value);
		}
	}

	public void SetCursor(bool value)
	{
		if (!cursorObject.IsNull())
		{
			cursorObject.gameObject.SetActive(value);
		}
	}

	public void AddListener(EventTriggerType type, Action<BaseEventData> action)
	{
		EventTrigger.Entry entry = new EventTrigger.Entry
		{
			eventID = type
		};
		entry.callback.AddListener(action.Invoke);
		EventTrigger.triggers.Add(entry);
	}

	public void AddListeners(IEnumerable<(EventTriggerType type, Action<BaseEventData> action)> listeners)
	{
		foreach (var (type, action) in listeners)
		{
			if (action != null)
			{
				AddListener(type, action);
			}
		}
	}

	private void InitializeButtonAction()
	{
		bool num = !GetComponent<Image>().IsNull();
		raycastTargetImage = base.gameObject.AddOrGetComponent<Image>();
		raycastTargetImage.raycastTarget = true;
		if (!num)
		{
			raycastTargetImage.color = GraphicHelper.TransparentColor;
		}
	}
}
