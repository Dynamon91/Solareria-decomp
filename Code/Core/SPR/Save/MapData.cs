using System;
using System.Collections.Generic;
using System.Linq;
using SPR.UI.Map;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class MapData
{
	[SerializeField]
	private List<MapIconData> activeNormalIcons = new List<MapIconData>();

	[SerializeField]
	private List<MapSpecialIconData> activeSpecialIcons = new List<MapSpecialIconData>();

	[SerializeField]
	private int lastNormalIndex = -1;

	[SerializeField]
	private List<MapObjectType> activeLegends = new List<MapObjectType>();

	[SerializeField]
	private bool watchedNewRelicLegend;

	[SerializeField]
	private List<string> spawnedByOtherItems = new List<string>();

	public IReadOnlyCollection<MapIconData> ActiveNormalIcons => activeNormalIcons;

	public IReadOnlyCollection<MapSpecialIconData> ActiveSpecialIcons => activeSpecialIcons;

	public IReadOnlyCollection<MapObjectType> ActiveLegends => activeLegends;

	public int LastNormalIndex
	{
		get
		{
			return lastNormalIndex;
		}
		set
		{
			lastNormalIndex = value;
		}
	}

	public bool WatchedNewRelicLegend
	{
		get
		{
			return watchedNewRelicLegend;
		}
		set
		{
			watchedNewRelicLegend = value;
		}
	}

	private MapIconData GetNormalData(int type)
	{
		for (int i = activeNormalIcons.Count; i < type; i++)
		{
			activeNormalIcons.Add(new MapIconData
			{
				type = -(i + 1),
				worldPositionConstants = new List<Vector2>()
			});
		}
		return activeNormalIcons[type - 1];
	}

	private void AdjustNormalType(ref int type)
	{
		if (type < 0)
		{
			type *= -1;
		}
		else
		{
			DebugLog.GeneralLogWarning($"일반 마커의 인덱스로 0보다 큰 값({type})을 받았습니다! 일단 그대로 진행합니다.");
		}
	}

	public List<Vector2> GetNormalIcons(int type)
	{
		AdjustNormalType(ref type);
		return GetNormalData(type).worldPositionConstants;
	}

	public int AddActiveNormalIcon(int type, Vector2 worldPosition)
	{
		AdjustNormalType(ref type);
		MapIconData normalData = GetNormalData(type);
		normalData.worldPositionConstants.Add(worldPosition);
		return normalData.worldPositionConstants.Count - 1;
	}

	public bool TryRemoveActiveNormalIcon(int type, int index)
	{
		AdjustNormalType(ref type);
		MapIconData normalData = GetNormalData(type);
		if (normalData.worldPositionConstants.Count < index)
		{
			DebugLog.GeneralLogError($"지도 마커 데이터(종류: {type})의 갯수({normalData.worldPositionConstants.Count})가 목표 인덱스({index})보다 적습니다!");
			return false;
		}
		normalData.worldPositionConstants.RemoveAt(index);
		return true;
	}

	public bool TryRemoveActiveNormalIcon(int type, Vector2 pos)
	{
		AdjustNormalType(ref type);
		return GetNormalData(type).worldPositionConstants.Remove(pos);
	}

	private MapSpecialIconData GetSpecialData(int specialType)
	{
		for (int i = activeSpecialIcons.Count; i < specialType; i++)
		{
			activeSpecialIcons.Add(new MapSpecialIconData
			{
				type = i + 1,
				status = MapMarkerStatus.Unavailable
			});
		}
		return activeSpecialIcons[specialType - 1];
	}

	public MapMarkerStatus GetSpecialStatus(int specialType)
	{
		return GetSpecialData(specialType).status;
	}

	public void SetSpecialStatus(int specialType, MapMarkerStatus status)
	{
		MapSpecialIconData specialData = GetSpecialData(specialType);
		specialData.type = specialType;
		specialData.status = status;
	}

	public bool HasAnySpecialMarker()
	{
		return activeSpecialIcons.Any((MapSpecialIconData data) => data.status != MapMarkerStatus.Unavailable);
	}

	public void AddActiveLegend(MapObjectType type)
	{
		if (!activeLegends.Contains(type))
		{
			activeLegends.Add(type);
		}
	}

	public void AddSpawnedItemByOther(string id)
	{
		if (!spawnedByOtherItems.Contains(id))
		{
			spawnedByOtherItems.Add(id);
		}
	}

	public bool IsItemSpawnedByOther(string id)
	{
		return spawnedByOtherItems.Contains(id);
	}
}
