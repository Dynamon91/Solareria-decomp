using SPR.ObjectPool;
using SPR.Save;
using UnityEngine;

namespace SPR.UI.Memory;

public class MemoryDataCell : MonoBehaviour, IPoolableObject
{
	[SerializeField]
	private LocaleText localeText;

	[SerializeField]
	private GameObject cursorObject;

	[SerializeField]
	private GameObject notiIcon;

	private RectTransform rectTransform;

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	public MemoryData MemoryData { get; private set; }

	public RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	public string PrefabKey => "Memory Data Cell.prefab";

	public GameObject GameObject => base.gameObject;

	public void SetData(MemoryData data)
	{
		MemoryData = data;
		SetNotiIconActive();
		if (data.IsUnknown)
		{
			localeText.SetData("Name_Unknown");
			localeText.SetAlpha(0.4f);
		}
		else
		{
			localeText.SetData(MenuMemoryPanel.GetTitleID(data));
			localeText.SetAlpha(1f);
		}
	}

	public void Focus(bool value)
	{
		if (value)
		{
			WanderingData.WatchMemory(MemoryData.Category, MemoryData.ID);
		}
		SetNotiIconActive();
		cursorObject.SetActive(value);
	}

	private void SetNotiIconActive()
	{
		bool notiIconActive = WanderingData.IsUnwatchedMemory(MemoryData.Category, MemoryData.ID);
		SetNotiIconActive(notiIconActive);
	}

	private void SetNotiIconActive(bool value)
	{
		notiIcon.SetActive(value);
	}

	public void InitializeBeforeUse()
	{
		SetNotiIconActive(value: false);
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
