using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class SectorData
{
	[Serializable]
	public class ObjectState
	{
		public string id;

		public int state;

		public bool watched;

		public ObjectState(string id, int state, bool watched = false)
		{
			this.id = id;
			this.state = state;
			this.watched = watched;
		}
	}

	[Serializable]
	public class MapFogData
	{
		public readonly int Index;

		public readonly List<int> RevealedFogs = new List<int>();

		public MapFogData(int index)
		{
			Index = index;
		}
	}

	[SerializeField]
	private SectorName sectorName;

	[SerializeField]
	private List<int> visitedFieldIDList;

	[SerializeField]
	private List<ObjectState> objectStates;

	[SerializeField]
	private List<string> dropItemIDList;

	[SerializeField]
	private List<int> interactedBonfireFieldIDList;

	[SerializeField]
	private Dictionary<int, Vector2> restedBonfireFieldIDList;

	[SerializeField]
	private List<MapFogData> revealedMapFogs;

	public SectorName SectorName => sectorName;

	public IReadOnlyList<int> VisitedFieldIDList => visitedFieldIDList;

	public IReadOnlyList<string> DropItemIDList => dropItemIDList ?? (dropItemIDList = new List<string>());

	public IReadOnlyList<int> InteractedBonfireFieldIDList => interactedBonfireFieldIDList;

	public IReadOnlyDictionary<int, Vector2> RestedBonfireFieldIDList => restedBonfireFieldIDList;

	public SectorData(SectorName sectorName)
	{
		this.sectorName = sectorName;
		visitedFieldIDList = new List<int>();
		interactedBonfireFieldIDList = new List<int>();
		objectStates = new List<ObjectState>();
		restedBonfireFieldIDList = new Dictionary<int, Vector2>();
		revealedMapFogs = new List<MapFogData>();
	}

	public void AddVisitedFieldID(int fieldID)
	{
		if (visitedFieldIDList == null)
		{
			visitedFieldIDList = new List<int>();
		}
		if (!visitedFieldIDList.Contains(fieldID))
		{
			visitedFieldIDList.Add(fieldID);
		}
	}

	public void AddInteractedBonfireFieldID(int fieldID)
	{
		if (interactedBonfireFieldIDList == null)
		{
			interactedBonfireFieldIDList = new List<int>();
		}
		if (!interactedBonfireFieldIDList.Contains(fieldID))
		{
			interactedBonfireFieldIDList.Add(fieldID);
		}
	}

	public void AddRestedBonfireFieldID(int fieldID, Vector2 position)
	{
		if (restedBonfireFieldIDList == null)
		{
			restedBonfireFieldIDList = new Dictionary<int, Vector2>();
		}
		restedBonfireFieldIDList.TryAdd(fieldID, position);
	}

	public void SaveObjectState(string id, int state, bool watched = false)
	{
		foreach (ObjectState objectState in objectStates)
		{
			if (objectState.id == id)
			{
				objectState.state = state;
				objectState.watched = watched;
				return;
			}
		}
		objectStates.Add(new ObjectState(id, state, watched));
	}

	public void SaveObjectStateWatched(string id, bool watched)
	{
		foreach (ObjectState objectState in objectStates)
		{
			if (objectState.id == id)
			{
				objectState.watched = watched;
				return;
			}
		}
		objectStates.Add(new ObjectState(id, 0, watched));
	}

	public bool TryGetObjectState(string id, out ObjectState objectState)
	{
		objectState = objectStates.FirstOrDefault((ObjectState os) => os.id == id);
		return objectState != null;
	}

	public bool TryGetObjectStateNum(string id, out int state)
	{
		ObjectState objectState = objectStates.FirstOrDefault((ObjectState os) => os.id == id);
		bool flag = objectState != null;
		state = (flag ? objectState.state : 0);
		return flag;
	}

	public bool TryGetObjectStateWatched(string id, out bool watched)
	{
		ObjectState objectState;
		bool flag = TryGetObjectState(id, out objectState);
		watched = flag && objectState.watched;
		return flag;
	}

	internal void RemoveObjectState(string id)
	{
		if (TryGetObjectState(id, out var objectState))
		{
			objectStates.Remove(objectState);
		}
	}

	public void AddDropItemID(string id)
	{
		if (dropItemIDList == null)
		{
			dropItemIDList = new List<string>();
		}
		if (!dropItemIDList.Contains(id))
		{
			dropItemIDList.Add(id);
		}
	}

	public void RemoveDropItemID(string id)
	{
		dropItemIDList?.Remove(id);
	}

	public bool IsFogRevealed(int sceneIndex, int fogIndex)
	{
		if (revealedMapFogs == null)
		{
			revealedMapFogs = new List<MapFogData>();
		}
		return revealedMapFogs.FirstOrDefault((MapFogData data) => data.Index == sceneIndex)?.RevealedFogs.Contains(fogIndex) ?? false;
	}

	public void AddRevealedFog(int sceneIndex, int fogIndex)
	{
		if (revealedMapFogs == null)
		{
			revealedMapFogs = new List<MapFogData>();
		}
		MapFogData mapFogData = revealedMapFogs.FirstOrDefault((MapFogData data) => data.Index == sceneIndex);
		if (mapFogData == null)
		{
			mapFogData = new MapFogData(sceneIndex);
			revealedMapFogs.Add(mapFogData);
		}
		mapFogData.RevealedFogs.Add(fogIndex);
	}
}
