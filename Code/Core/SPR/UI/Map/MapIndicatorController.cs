using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SPR.UI.Map;

public class MapIndicatorController : MonoBehaviour
{
	private readonly Dictionary<MapObjectIcon, MapIndicator> indicators = new Dictionary<MapObjectIcon, MapIndicator>();

	private CanvasGroup canvasGroup;

	public CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	public bool Contains(MapObjectIcon icon)
	{
		return indicators.ContainsKey(icon);
	}

	public void Clear()
	{
		foreach (KeyValuePair<MapObjectIcon, MapIndicator> item in indicators.ToList())
		{
			indicators.Remove(item.Key);
			item.Value.ReturnToPool();
		}
	}

	public void Add(MapObjectIcon unwatched)
	{
		MapIndicator value = Game.Instance.ObjectPoolManager.GetObject<MapIndicator>("Map Indicator.prefab", base.transform);
		indicators.Add(unwatched, value);
	}

	public bool TryRemove(MapObjectIcon watched)
	{
		if (!Contains(watched))
		{
			return false;
		}
		indicators[watched].ReturnToPool();
		indicators.Remove(watched);
		return true;
	}

	public void UpdateIndicators(Vector2 mapPos, float scale)
	{
		foreach (var (mapObjectIcon2, mapIndicator2) in indicators)
		{
			if (IsFar(mapObjectIcon2.RectTransform.anchoredPosition * scale + mapPos, out var result))
			{
				mapIndicator2.SetActive(value: true);
				mapIndicator2.SetPosition(result);
			}
			else
			{
				mapIndicator2.SetActive(value: false);
			}
		}
	}

	private static bool IsFar(Vector2 pv, out Vector2 result)
	{
		result = Vector2.zero;
		float num = Mathf.Abs(pv.x) / 600f;
		float num2 = Mathf.Abs(pv.y) / 435f;
		bool num3 = num > 1f;
		bool flag = num2 > 1f;
		if (num3)
		{
			if (flag)
			{
				result = pv / Mathf.Max(num, num2);
				return true;
			}
			result = pv / num;
			return true;
		}
		if (flag)
		{
			result = pv / num2;
			return true;
		}
		return false;
	}
}
