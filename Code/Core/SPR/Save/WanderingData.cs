using System;
using System.Collections.Generic;
using System.Linq;
using Library;
using SPR.Library;
using SPR.UI.Memory;
using SPR.UI.Tutorial;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class WanderingData
{
	[SerializeField]
	private List<ActionName> doneTutorials;

	[SerializeField]
	private List<TutorialMessageData> tutorialMessageData;

	[SerializeField]
	private List<string> knownInfoStonesData;

	[SerializeField]
	private List<string> unwatchedInfoStoneIcons;

	[SerializeField]
	private DictionaryWrapper<string, WatchedState> unwatchedInventoryItems;

	[SerializeField]
	private List<CutsceneData> watchedCutsceneData;

	[SerializeField]
	private DictionaryListValueWrapper<MemoryCategory, MemoryNotiData> unwatchedMemoryData;

	[SerializeField]
	private Queue<string> haechiRewardItems;

	[SerializeField]
	private int haechiRewardIndex;

	[SerializeField]
	private int totalDonation;

	[SerializeField]
	private List<HeatSkillType> watchedHeatSkillTypes;

	[SerializeField]
	private DictionaryListValueWrapper<PartType, string> unwatchedParts;

	[SerializeField]
	private List<int> unwatchedCoreStones;

	[SerializeField]
	private List<ActionName> unwatchedTutorialsInOption;

	[SerializeField]
	private int worldsEndBgmParameter;

	[SerializeField]
	private Queue<string> resonancesAfterMoveScene;

	private readonly Dictionary<string, List<FieldID>> resonanceExceptionDict = new Dictionary<string, List<FieldID>> { 
	{
		"HaechiRescue_Resonance",
		new List<FieldID>
		{
			new FieldID(SectorName.FadedCastle, 23),
			new FieldID(SectorName.AshHills, 26),
			new FieldID(SectorName.Library, 1)
		}
	} };

	public int TotalDonation => totalDonation;

	public event Action OnUnwatchedPartDataChanged = delegate
	{
	};

	public event Action OnUnwatchedInventoryDataChanged = delegate
	{
	};

	public event Action OnUnwatchedIconDataChanged = delegate
	{
	};

	public event Action OnUnwatchedMemoryDataChanged = delegate
	{
	};

	public event Action OnUnwatchedCoreStoneDataChanged = delegate
	{
	};

	public WanderingData()
	{
		doneTutorials = new List<ActionName>();
		tutorialMessageData = new List<TutorialMessageData>();
		knownInfoStonesData = new List<string>();
		unwatchedInfoStoneIcons = new List<string>();
		unwatchedInventoryItems = new DictionaryWrapper<string, WatchedState>();
		watchedCutsceneData = new List<CutsceneData>();
		unwatchedMemoryData = new DictionaryListValueWrapper<MemoryCategory, MemoryNotiData>();
		haechiRewardItems = new Queue<string>();
		watchedHeatSkillTypes = new List<HeatSkillType>();
		unwatchedCoreStones = new List<int>();
		unwatchedTutorialsInOption = new List<ActionName>();
		unwatchedParts = new DictionaryListValueWrapper<PartType, string>();
		for (int num = 0; num < 4; num++)
		{
			unwatchedParts.AddKey((PartType)num);
		}
		worldsEndBgmParameter = 0;
		resonancesAfterMoveScene = new Queue<string>();
	}

	public bool ShouldShowTutorial(ActionName actionName)
	{
		return !doneTutorials.Contains(actionName);
	}

	public bool ShouldShowNewTutorialMessage(ActionName actionName)
	{
		TutorialMessageData tutorialMessageData = GetTutorialMessageData(actionName);
		if (tutorialMessageData != null)
		{
			return tutorialMessageData.State == TutorialState.None;
		}
		return true;
	}

	public bool ShouldShowTutorialMessage(ActionName actionName)
	{
		TutorialMessageData tutorialMessageData = GetTutorialMessageData(actionName);
		if (tutorialMessageData != null)
		{
			return tutorialMessageData.State != TutorialState.Done;
		}
		return true;
	}

	public void SetTutorialDone(ActionName actionName)
	{
		doneTutorials.Add(actionName);
		AddUnwatchedTutorialInOption(actionName);
	}

	public void SetTutorialMessageProgressing(ActionName actionName)
	{
		SetTutorialMessage(actionName, TutorialState.Progressing);
	}

	public void SetTutorialMessageDone(ActionName actionName)
	{
		SetTutorialMessage(actionName, TutorialState.Done);
	}

	public IEnumerable<ActionName> GetProgressingTutorialMessages()
	{
		return from data in tutorialMessageData
			where data.State == TutorialState.Progressing
			select data.Action;
	}

	public TutorialMessageData GetTutorialMessageData(ActionName actionName)
	{
		return tutorialMessageData.SingleOrDefault((TutorialMessageData data) => data.Action == actionName);
	}

	private void SetTutorialMessage(ActionName actionName, TutorialState tutorialState)
	{
		TutorialMessageData tutorialMessageData = GetTutorialMessageData(actionName);
		if (tutorialMessageData == null)
		{
			this.tutorialMessageData.Add(new TutorialMessageData(actionName, tutorialState));
		}
		else
		{
			tutorialMessageData.SetState(tutorialState);
		}
	}

	public bool HasKnownInfoStone(string id)
	{
		return knownInfoStonesData.Contains(id);
	}

	public bool AddKnownInfoStone(string id)
	{
		if (HasKnownInfoStone(id))
		{
			return false;
		}
		knownInfoStonesData.Add(id);
		return true;
	}

	public int CountUnwatchedIcons()
	{
		return unwatchedInfoStoneIcons.Where((string id) => !Trigger.CheckTrigger("Trigger_" + id)).ToList().Count();
	}

	public bool IsUnwatchedInfoStoneIcon(string id)
	{
		return unwatchedInfoStoneIcons.Contains(id);
	}

	public void AddUnwatchedInfoStoneIcon(string id)
	{
		unwatchedInfoStoneIcons.Add(id);
		this.OnUnwatchedIconDataChanged?.Invoke();
	}

	public bool RemoveUnwatchedInfoStoneIcon(string id)
	{
		if (!unwatchedInfoStoneIcons.Contains(id))
		{
			return false;
		}
		bool result = unwatchedInfoStoneIcons.Remove(id);
		Action action = this.OnUnwatchedIconDataChanged;
		if (action != null)
		{
			action();
			return result;
		}
		return result;
	}

	public void UpdateUnwatchedInfoStoneIcon()
	{
		this.OnUnwatchedIconDataChanged?.Invoke();
	}

	public int CountUnwatchedInventoryItems()
	{
		return unwatchedInventoryItems.KeyValuePairs.Count((DictionaryWrapper<string, WatchedState>.KeyValueSet kvp) => kvp.Value == WatchedState.Unwatched);
	}

	public bool HasUnwatchedInventoryItem(ItemType type)
	{
		return unwatchedInventoryItems.KeyValuePairs.Any((DictionaryWrapper<string, WatchedState>.KeyValueSet kvp) => Game.DataManager.GetItemData(kvp.Key).ItemType == type && kvp.Value == WatchedState.Unwatched);
	}

	public bool IsUnwatchedInventoryItem(string id)
	{
		if (unwatchedInventoryItems.TryGetValue(id, out var value))
		{
			return value == WatchedState.Unwatched;
		}
		return false;
	}

	public void AddUnwatchedInventoryItem(string id, bool invokeChanged = true)
	{
		if (!unwatchedInventoryItems.ContainsKey(id))
		{
			unwatchedInventoryItems.AddKey(id);
			if (invokeChanged)
			{
				this.OnUnwatchedInventoryDataChanged?.Invoke();
			}
		}
	}

	public void WatchInventoryItem(string id)
	{
		if (unwatchedInventoryItems.ContainsKey(id))
		{
			unwatchedInventoryItems[id] = WatchedState.Watched;
			this.OnUnwatchedInventoryDataChanged?.Invoke();
		}
	}

	public void RemoveUnwatchedInventoryItem(string id)
	{
		if (unwatchedInventoryItems.TryGetValue(id, out var value) && value != WatchedState.Watched)
		{
			unwatchedInventoryItems.RemoveKey(id);
			this.OnUnwatchedInventoryDataChanged?.Invoke();
		}
	}

	public void AddWatchedCutscene(string name, int state)
	{
		CutsceneData cutsceneData = watchedCutsceneData.FirstOrDefault((CutsceneData d) => d.CutsceneName == name);
		if (cutsceneData == null)
		{
			cutsceneData = new CutsceneData(name, state);
			watchedCutsceneData.Add(cutsceneData);
		}
		else
		{
			cutsceneData.AddWatchedIndex(state);
		}
	}

	internal void RemoveWatchedCutscene(string name)
	{
		CutsceneData cutsceneData = watchedCutsceneData.FirstOrDefault((CutsceneData d) => d.CutsceneName == name);
		if (cutsceneData != null)
		{
			watchedCutsceneData.Remove(cutsceneData);
		}
	}

	public bool IsWatchedCutscene(string name)
	{
		return watchedCutsceneData.FirstOrDefault((CutsceneData d) => d.CutsceneName == name) != null;
	}

	public bool IsWatchedCutscene(string name, int state)
	{
		CutsceneData cutsceneData = watchedCutsceneData.FirstOrDefault((CutsceneData d) => d.CutsceneName == name);
		if (cutsceneData != null)
		{
			if (state >= 0)
			{
				return cutsceneData.Contains(state);
			}
			return true;
		}
		return false;
	}

	public int CountUnwatchedMemories()
	{
		return unwatchedMemoryData.KeyValuePairs.SelectMany((DictionaryWrapper<MemoryCategory, List<MemoryNotiData>>.KeyValueSet kvp) => kvp.Value).Count((MemoryNotiData d) => d.IsUnwatched);
	}

	public bool AddUnwatchedMemory(MemoryCategory category, string id, bool reset = false)
	{
		if (!unwatchedMemoryData.TryGetValue(category, out var value))
		{
			unwatchedMemoryData.AddKey(category);
			value = unwatchedMemoryData[category];
		}
		MemoryNotiData memoryNotiData = value.FirstOrDefault((MemoryNotiData data) => data.Id == id);
		if (memoryNotiData != null)
		{
			if (!reset)
			{
				return false;
			}
			memoryNotiData.SetUnwatched();
			this.OnUnwatchedMemoryDataChanged?.Invoke();
			return true;
		}
		unwatchedMemoryData.AddValue(category, new MemoryNotiData(id));
		this.OnUnwatchedMemoryDataChanged?.Invoke();
		return true;
	}

	public void WatchMemory(MemoryCategory category, string id)
	{
		if (unwatchedMemoryData.TryGetValue(category, out var value))
		{
			MemoryNotiData memoryNotiData = value.FirstOrDefault((MemoryNotiData d) => d.Id == id);
			if (memoryNotiData != null)
			{
				memoryNotiData.SetWatched();
				this.OnUnwatchedMemoryDataChanged?.Invoke();
			}
		}
	}

	public IReadOnlyList<string> GetUnwatchedMemories(MemoryCategory category)
	{
		if (!unwatchedMemoryData.TryGetValue(category, out var value))
		{
			return new List<string>();
		}
		return (from d in value
			where d.IsUnwatched
			select d.Id).ToList();
	}

	public bool IsUnwatchedMemory(MemoryCategory category, string id)
	{
		if (unwatchedMemoryData.TryGetValue(category, out var value) && value.Contains((MemoryNotiData d) => d.Id == id, out var result))
		{
			return result.IsUnwatched;
		}
		return false;
	}

	public void AddNewHaechiReward(bool isFiveWarriors)
	{
		haechiRewardItems.Enqueue(isFiveWarriors ? "SupremeSolinGem" : "SuperiorSolinMass");
	}

	public bool HasHaechiReward()
	{
		return haechiRewardItems.Count > 0;
	}

	public IReadOnlyList<string> GetHaechiRewards()
	{
		List<string> list = new List<string>();
		for (int i = 0; i < 3; i++)
		{
			if (!haechiRewardItems.TryDequeue(out var result))
			{
				break;
			}
			list.Add(result);
		}
		return list;
	}

	public void AddTotalDonation(int value)
	{
		totalDonation += value;
	}

	public void AddWatchedHeatSkillType(HeatSkillType type)
	{
		if (!watchedHeatSkillTypes.Contains(type))
		{
			watchedHeatSkillTypes.Add(type);
		}
	}

	public bool IsWatchedHeatSkillType(HeatSkillType type)
	{
		return watchedHeatSkillTypes.Contains(type);
	}

	public void AddUnwatchedPart(PartType partType, string id)
	{
		unwatchedParts[partType].Add(id);
		this.OnUnwatchedPartDataChanged?.Invoke();
	}

	public void RemoveUnwatchedPart(PartType partType, string id)
	{
		if (unwatchedParts.ContainsKey(partType))
		{
			unwatchedParts[partType].Remove(id);
			this.OnUnwatchedPartDataChanged?.Invoke();
		}
	}

	public bool HasUnwatchedPart(PartType partType, string id)
	{
		return unwatchedParts.ContainsValue(partType, id);
	}

	public bool HasUnwatchedPart(PartType partType)
	{
		return unwatchedParts.HasValue(partType);
	}

	public bool HasAnyUnwatchedPart()
	{
		return unwatchedParts.ValueCount > 0;
	}

	public void AddUnwatchedCoreStone(int id, bool invokeChanged = true)
	{
		if (!unwatchedCoreStones.Contains(id))
		{
			unwatchedCoreStones.Add(id);
			if (invokeChanged)
			{
				this.OnUnwatchedCoreStoneDataChanged?.Invoke();
			}
		}
	}

	public void RemoveUnwatchedCoreStone(int id, bool invokeChanged = true)
	{
		unwatchedCoreStones.Remove(id);
		this.OnUnwatchedCoreStoneDataChanged?.Invoke();
	}

	public bool IsUnwatchedCoreStone(int id)
	{
		return unwatchedCoreStones.Contains(id);
	}

	public bool HasUnwatchedCoreStone()
	{
		return unwatchedCoreStones.Count > 0;
	}

	public void AddUnwatchedTutorialInOption(ActionName actionName)
	{
		unwatchedTutorialsInOption.Add(actionName);
	}

	public void RemoveUnwatchedTutorialInOption(ActionName actionName)
	{
		unwatchedTutorialsInOption.Remove(actionName);
	}

	public bool IsUnwatchedTutorialInOption(ActionName actionName)
	{
		return unwatchedTutorialsInOption.Contains(actionName);
	}

	public int GetWorldsEndBgmParameter()
	{
		return worldsEndBgmParameter;
	}

	public void SetWorldsEndBgmParameter(int value)
	{
		worldsEndBgmParameter = value;
	}

	public void EnqueueResonanceAfterMoveScene(string groupId)
	{
		resonancesAfterMoveScene.Enqueue(groupId);
	}

	public bool TryDequeueResonanceAfterMoveScene(FieldID fieldID, out string groupId)
	{
		if (!resonancesAfterMoveScene.TryPeek(out groupId))
		{
			return false;
		}
		if (resonanceExceptionDict.TryGetValue(groupId, out var value) && value.Contains(fieldID))
		{
			return false;
		}
		resonancesAfterMoveScene.Dequeue();
		return true;
	}
}
