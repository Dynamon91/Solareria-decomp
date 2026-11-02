using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class TalkData
{
	[Serializable]
	public class InteractCountData
	{
		public ActorName npcName;

		[SerializeField]
		private List<FieldID> fields = new List<FieldID>();

		public IReadOnlyList<FieldID> Fields => fields;

		public IReadOnlyList<SectorName> Sectors => fields.Select((FieldID id) => id.SectorName).Distinct().ToList();

		public int Count => fields.Count;

		public int UniqueCount => fields.Distinct().Count();

		public InteractCountData(ActorName npcName, FieldID field)
		{
			this.npcName = npcName;
			fields.Add(field);
		}

		public void AddField(FieldID fieldID)
		{
			if (!fields.Contains(fieldID))
			{
				fields.Add(fieldID);
			}
		}
	}

	[SerializeField]
	private List<string> watchedScenarioIDList = new List<string>();

	[SerializeField]
	private List<InteractCountData> interactCountDataList = new List<InteractCountData>();

	public void AddTalk(string scenarioID)
	{
		if (watchedScenarioIDList == null)
		{
			watchedScenarioIDList = new List<string>();
		}
		watchedScenarioIDList.Add(scenarioID);
	}

	internal void RemoveTalk(string scenarioID)
	{
		watchedScenarioIDList.Remove(scenarioID);
	}

	public int GetTalkCount(string scenarioID)
	{
		if (watchedScenarioIDList == null)
		{
			watchedScenarioIDList = new List<string>();
		}
		return watchedScenarioIDList.FindAll((string x) => x == scenarioID).Count;
	}

	public void AddInteractCount(ActorName npcName, FieldID fieldID)
	{
		if (interactCountDataList == null)
		{
			interactCountDataList = new List<InteractCountData>();
		}
		InteractCountData interactCountData = interactCountDataList.Find((InteractCountData x) => x.npcName == npcName);
		if (interactCountData == null)
		{
			interactCountDataList.Add(new InteractCountData(npcName, fieldID));
		}
		else
		{
			interactCountData.AddField(fieldID);
		}
	}

	public InteractCountData GetInteractData(ActorName npcName)
	{
		if (interactCountDataList == null)
		{
			interactCountDataList = new List<InteractCountData>();
		}
		return interactCountDataList.Find((InteractCountData x) => x.npcName == npcName);
	}

	public int GetInteractCount(ActorName npcName)
	{
		return GetInteractData(npcName)?.Count ?? 0;
	}
}
