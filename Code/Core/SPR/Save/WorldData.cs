using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class WorldData
{
	[SerializeField]
	private List<SectorData> sectorDataList = new List<SectorData>();

	[SerializeField]
	private List<SectorName> shownFirstTextList = new List<SectorName>();

	[SerializeField]
	private List<FieldID> showSpecialFirstTextList = new List<FieldID>();

	[SerializeField]
	private StoreData storeData = new StoreData();

	[SerializeField]
	private Dictionary<FieldID, List<Vector2>> eliminatedCreatures = new Dictionary<FieldID, List<Vector2>>();

	[SerializeField]
	private Dictionary<FieldID, List<Vector2>> ambushEliminatedCreature = new Dictionary<FieldID, List<Vector2>>();

	[SerializeField]
	private List<string> fieldInteractList = new List<string>();

	[SerializeField]
	private MapData mapData = new MapData();

	[SerializeField]
	private Dictionary<ActorName, FieldID> restNpcFirstList = new Dictionary<ActorName, FieldID>();

	public StoreData StoreData => storeData;

	public MapData MapData => mapData;

	public bool HasShownSector(SectorName sectorName)
	{
		return shownFirstTextList.Contains(sectorName);
	}

	public void AddShownSector(SectorName sectorName)
	{
		if (shownFirstTextList.Contains(sectorName))
		{
			if (sectorName != SectorName.SilentRuins)
			{
				DebugLog.SaveDataLogError($"해당 지역({sectorName})은 이미 최초 텍스트를 보여줬습니다!");
			}
		}
		else
		{
			shownFirstTextList.Add(sectorName);
		}
	}

	public bool HasShownSpecialSector(FieldID fieldID)
	{
		return showSpecialFirstTextList.Contains(fieldID);
	}

	public void AddShownSpecialSector(FieldID fieldID)
	{
		if (showSpecialFirstTextList.Contains(fieldID))
		{
			DebugLog.SaveDataLogError($"해당 지역({fieldID})은 이미 최초 텍스트를 보여줬습니다!");
		}
		else
		{
			showSpecialFirstTextList.Add(fieldID);
		}
	}

	public bool TryGetSectorData(out SectorData data)
	{
		return TryGetSectorData(Game.Instance.CurrentFieldID.SectorName, out data);
	}

	public bool TryGetSectorData(SectorName sectorName, out SectorData data)
	{
		if (sectorName == SectorName.None)
		{
			data = null;
			DebugLog.GeneralLogWarning("SectorName이 None입니다! SectorData를 null로 반환합니다.");
			return false;
		}
		foreach (SectorData sectorData in sectorDataList)
		{
			if (sectorData.SectorName == sectorName)
			{
				data = sectorData;
				return true;
			}
		}
		data = new SectorData(sectorName);
		sectorDataList.Add(data);
		return data != null;
	}

	public void AddVisitedFieldID(FieldID fieldID)
	{
		if (!TryGetSectorData(fieldID.SectorName, out var data))
		{
			DebugLog.SaveDataLogError($"{fieldID.SectorName}에 해당하는 SectorData가 없습니다. Field 방문 정보를 추가하지 못했습니다! fieldID: {fieldID}");
		}
		else
		{
			data.AddVisitedFieldID(fieldID.SceneIndex);
		}
	}

	public void AddInteractedBonfireFieldID(FieldID fieldID)
	{
		if (!TryGetSectorData(fieldID.SectorName, out var data))
		{
			DebugLog.SaveDataLogError($"{fieldID.SectorName}에 해당하는 SectorData가 없습니다. 화톳불 상호작용 정보를 추가하지 못했습니다! fieldID: {fieldID}");
		}
		else
		{
			data.AddInteractedBonfireFieldID(fieldID.SceneIndex);
		}
	}

	public void AddRestedBonfireFieldID(FieldID fieldID, Vector2 position)
	{
		if (!TryGetSectorData(fieldID.SectorName, out var data))
		{
			DebugLog.SaveDataLogError($"{fieldID.SectorName}에 해당하는 SectorData가 없습니다. 화톳불 상호작용 정보를 추가하지 못했습니다! fieldID: {fieldID}");
		}
		else
		{
			data.AddRestedBonfireFieldID(fieldID.SceneIndex, position);
		}
	}

	public int GetVisitedFieldCount(SectorName sector)
	{
		if (!TryGetSectorData(sector, out var data))
		{
			return 0;
		}
		return data.VisitedFieldIDList.Count;
	}

	public bool IsVisitedField(FieldID fieldID)
	{
		if (TryGetSectorData(fieldID.SectorName, out var data))
		{
			return data.VisitedFieldIDList.Contains(fieldID.SceneIndex);
		}
		return false;
	}

	public bool HasInteractedBonfire(FieldID fieldID)
	{
		if (TryGetSectorData(fieldID.SectorName, out var data))
		{
			return data.InteractedBonfireFieldIDList.Contains(fieldID.SceneIndex);
		}
		return false;
	}

	public bool HasRestedBonfire(FieldID fieldID)
	{
		if (TryGetSectorData(fieldID.SectorName, out var data))
		{
			return data.RestedBonfireFieldIDList.ContainsKey(fieldID.SceneIndex);
		}
		return false;
	}

	public void AddEliminatedCreature(FieldID fieldID, Vector2 spawnPosition)
	{
		eliminatedCreatures.TryAdd(fieldID, new List<Vector2>());
		if (!eliminatedCreatures[fieldID].Contains(spawnPosition))
		{
			eliminatedCreatures[fieldID].Add(spawnPosition);
		}
	}

	public bool IsEliminatedCreature(FieldID fieldID, Vector2 spawnPosition)
	{
		if (eliminatedCreatures.ContainsKey(fieldID))
		{
			return eliminatedCreatures[fieldID].Contains(spawnPosition);
		}
		return false;
	}

	public void AddEliminatedAmbushCreature(FieldID fieldID, Vector2 spawnPosition)
	{
		ambushEliminatedCreature.TryAdd(fieldID, new List<Vector2>());
		if (!ambushEliminatedCreature[fieldID].Contains(spawnPosition))
		{
			ambushEliminatedCreature[fieldID].Add(spawnPosition);
		}
	}

	public bool IsEliminatedAmbushCreature(FieldID fieldID, Vector2 spawnPosition)
	{
		if (ambushEliminatedCreature.ContainsKey(fieldID))
		{
			return ambushEliminatedCreature[fieldID].Contains(spawnPosition);
		}
		return false;
	}

	public void AddInteractedFieldObject(string id)
	{
		if (!fieldInteractList.Contains(id))
		{
			fieldInteractList.Add(id);
		}
	}

	public bool HasInteractedFieldObject(string id)
	{
		return fieldInteractList.Contains(id);
	}

	public Vector2 GetRestedPosition(FieldID fieldID)
	{
		if (TryGetSectorData(fieldID.SectorName, out var data) && data.RestedBonfireFieldIDList.TryGetValue(fieldID.SceneIndex, out var value))
		{
			return value;
		}
		DebugLog.SaveDataLogError("해당 화톳불에서 휴식한 위치가 기록되지 않았습니다");
		return Vector2.zero;
	}

	public bool TryGetObjectStateData(SectorName sectorName, string id, out SectorData.ObjectState objectState)
	{
		objectState = null;
		if (TryGetSectorData(sectorName, out var data))
		{
			return data.TryGetObjectState(id, out objectState);
		}
		return false;
	}

	public bool TryGetObjectState(SectorName sectorName, string id, out int state)
	{
		state = 0;
		if (TryGetSectorData(sectorName, out var data))
		{
			return data.TryGetObjectStateNum(id, out state);
		}
		return false;
	}

	public bool TryGetObjectState(string id, out int state)
	{
		state = 0;
		foreach (SectorData sectorData in sectorDataList)
		{
			if (sectorData.TryGetObjectStateNum(id, out state))
			{
				return true;
			}
		}
		return false;
	}

	public bool TryAddFirstRestNpc(ActorName name, FieldID fieldID)
	{
		return restNpcFirstList.TryAdd(name, fieldID);
	}

	public bool IsFirstRestNpc(ActorName name, FieldID fieldID)
	{
		if (restNpcFirstList.TryGetValue(name, out var value))
		{
			return value.Equals(fieldID);
		}
		return false;
	}
}
