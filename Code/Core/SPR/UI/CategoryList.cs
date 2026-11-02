using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SPR.UI;

public abstract class CategoryList : MonoBehaviour
{
	[SerializeField]
	protected List<UICategory> categories;

	[SerializeField]
	private CanvasGroup canvasGroup;

	public CanvasGroup CanvasGroup => canvasGroup;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public void Initialize()
	{
		for (int i = 0; i < categories.Count; i++)
		{
			int i2 = i;
			categories[i].Initialize(delegate(UICategory category)
			{
				SubscribeNotiCondition(category, i2);
			});
			categories[i].AddListeners(EventActions(i2));
		}
	}

	public virtual void OnActivate(bool isRest)
	{
	}

	public void Focus(int index, bool sound = true)
	{
		bool flag = false;
		for (int i = 0; i < categories.Count; i++)
		{
			bool flag2 = i == index;
			categories[i].SetFocus(flag2);
			flag = flag || flag2;
		}
		if (sound && flag)
		{
			SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Sub_Move");
		}
	}

	public void SetCursor(int index)
	{
		for (int i = 0; i < categories.Count; i++)
		{
			categories[i].SetCursor(i == index);
		}
	}

	public void SetWidth(float width)
	{
		categories.ForEach(delegate(UICategory c)
		{
			if (!(c.transform.parent != base.transform))
			{
				Vector2 sizeDelta = c.RectTransform.sizeDelta;
				c.RectTransform.sizeDelta = new Vector2(width, sizeDelta.y);
			}
		});
	}

	protected abstract void SubscribeNotiCondition(UICategory category, int index);

	protected abstract IEnumerable<(EventTriggerType type, Action<BaseEventData> action)> EventActions(int index);
}
