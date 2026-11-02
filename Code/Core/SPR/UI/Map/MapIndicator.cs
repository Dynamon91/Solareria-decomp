using SPR.ObjectPool;
using UnityEngine;

namespace SPR.UI.Map;

public class MapIndicator : MonoBehaviour, IPoolableObject
{
	private RectTransform rectTransform;

	private RectTransform iconTransform;

	public string PrefabKey => "Map Indicator.prefab";

	public GameObject GameObject => base.gameObject;

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
		if ((object)rectTransform == null)
		{
			rectTransform = GetComponent<RectTransform>();
		}
		if ((object)iconTransform == null)
		{
			iconTransform = base.transform.GetChild(0).GetComponent<RectTransform>();
		}
	}

	public void ReturnToPool()
	{
		base.gameObject.SetActive(value: false);
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void SetActive(bool value)
	{
		base.gameObject.SetActive(value);
	}

	public void SetPosition(Vector2 v)
	{
		rectTransform.anchoredPosition = v;
		rectTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * 57.29578f);
		iconTransform.rotation = Quaternion.identity;
	}
}
