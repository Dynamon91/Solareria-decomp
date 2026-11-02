using System;
using System.Collections.Generic;
using System.Linq;
using SPR.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Map;

public class MapLegend : MonoBehaviour
{
	[Serializable]
	private struct LegendObjectPair
	{
		public MapObjectType type;

		public GameObject gameObject;
	}

	[SerializeField]
	private GameObject spiritLegendObject;

	[SerializeField]
	private List<LegendObjectPair> legendObjects;

	[SerializeField]
	private List<MapLegendObject> mapLegendObjects;

	private RectTransform rect;

	private CanvasGroup canvasGroup;

	private RectTransform RectTransform => rect ?? (rect = GetComponent<RectTransform>());

	public CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private static MapData MapData => Game.Instance.SaveData.WorldData.MapData;

	public void SetLegend(bool spirit)
	{
		spiritLegendObject.SetActive(spirit);
		SetType(MapObjectType.FiveWarriors, out var _);
		bool result2;
		MapLegendObject mapLegendObject = SetType(MapObjectType.Relics, out result2);
		if (result2 && !MapData.WatchedNewRelicLegend)
		{
			mapLegendObject.ShowNew(value: true);
			MapData.WatchedNewRelicLegend = true;
		}
		else
		{
			mapLegendObject.ShowNew(value: false);
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
	}

	public MapLegendObject SetType(MapObjectType type, out bool result)
	{
		MapLegendObject mapLegendObject = mapLegendObjects.First((MapLegendObject pair) => pair.Type == type);
		result = MapData.ActiveLegends.Contains(type);
		mapLegendObject.SetActive(result);
		return mapLegendObject;
	}
}
