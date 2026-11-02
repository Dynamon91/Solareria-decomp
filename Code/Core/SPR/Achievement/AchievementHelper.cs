using System.Collections.Generic;

namespace SPR.Achievement;

public static class AchievementHelper
{
	private static IAchievementProvider currentProvider;

	private static readonly List<AchievementEntry> AchievementTable = new List<AchievementEntry>();

	public static bool IsInitialized { get; private set; } = false;

	public static void Initialize()
	{
		if (!IsInitialized)
		{
			InitializeProvider();
			BuildAchievementTable();
			IsInitialized = true;
			DebugLog.AchievementLog("AchievementHelper 초기화 완료");
		}
	}

	public static void CheckTriggers()
	{
		if (!IsInitialized)
		{
			return;
		}
		foreach (AchievementEntry item in AchievementTable)
		{
			if (!item.IsAchieved && Trigger.CheckTrigger(item.TriggerId))
			{
				Unlock(item.ID);
			}
		}
	}

	public static void Unlock(string achievementId)
	{
		if (IsInitialized)
		{
			AchievementEntry achievementEntry = AchievementTable.Find((AchievementEntry x) => x.ID == achievementId);
			if (achievementEntry != null && !achievementEntry.IsAchieved)
			{
				achievementEntry.SetAchieved(achieved: true);
				DebugLog.AchievementLog("업적 달성 로컬 저장 : " + achievementId);
				currentProvider?.UnlockAchievement(achievementId);
			}
		}
	}

	public static bool IsUnlocked(string achievementId)
	{
		if (!IsInitialized)
		{
			return false;
		}
		return AchievementTable.Find((AchievementEntry x) => x.ID == achievementId)?.IsAchieved ?? false;
	}

	private static void BuildAchievementTable()
	{
		AchievementTable.Clear();
		foreach (TriggerData allAchievementTriggerData in Game.DataManager.GetAllAchievementTriggerDataList())
		{
			if (IsValidAchievementTrigger(allAchievementTriggerData.ID, out var achievementID))
			{
				AchievementEntry item = new AchievementEntry(achievementID);
				AchievementTable.Add(item);
			}
		}
		DebugLog.AchievementLog($"업적 테이블 생성 완료: {AchievementTable.Count}개 등록됨");
	}

	private static void InitializeProvider()
	{
		currentProvider = new SteamAchievementProvider();
		currentProvider.Initialize();
		DebugLog.AchievementLog("업적 시스템 초기화 완료: " + currentProvider.GetType().Name);
	}

	private static bool IsValidAchievementTrigger(string triggerID, out string achievementID)
	{
		if (string.IsNullOrEmpty(triggerID))
		{
			achievementID = string.Empty;
			return false;
		}
		string[] array = triggerID.Split('_');
		if (array.Length != 3)
		{
			achievementID = string.Empty;
			return false;
		}
		achievementID = array[2];
		return true;
	}
}
