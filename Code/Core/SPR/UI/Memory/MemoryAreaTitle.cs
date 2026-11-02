using SPR.L10N;
using SPR.ObjectPool;
using SPR.Save;
using UnityEngine;

namespace SPR.UI.Memory;

public class MemoryAreaTitle : MonoBehaviour, IPoolableObject
{
	[SerializeField]
	private LocaleText localeText;

	[SerializeField]
	private GameObject notiIcon;

	private static WorldData WorldData => Game.Instance.SaveData.WorldData;

	public string PrefabKey => "Memory Area Title.prefab";

	public GameObject GameObject => base.gameObject;

	public void SetData(SectorName sectorName)
	{
		SetNotiIconActive();
		if (!WorldData.HasShownSector(sectorName))
		{
			localeText.SetData("Name_Unknown");
			localeText.SetAlpha(0.4f);
		}
		else
		{
			localeText.SetData(LocaleKey.SectorName(sectorName));
			localeText.SetAlpha(1f);
		}
	}

	private void SetNotiIconActive()
	{
		bool notiIconActive = false;
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
