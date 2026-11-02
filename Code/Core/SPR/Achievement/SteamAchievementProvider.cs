using Steamworks;

namespace SPR.Achievement;

public class SteamAchievementProvider : IAchievementProvider
{
	private bool isInitialized;

	public void Initialize()
	{
		if (!isInitialized)
		{
			isInitialized = true;
		}
	}

	public void UnlockAchievement(string achievementId)
	{
		bool pbAchieved;
		if (!isInitialized)
		{
			DebugLog.AchievementLog("SteamAchivementProvider가 초기화되지 않아 업적 해제를 진행할 수 없습니다.");
		}
		else if (string.IsNullOrWhiteSpace(achievementId))
		{
			DebugLog.AchievementLog("Steam achievementId가 비어 있습니다.");
		}
		else if (!SteamUserStats.GetAchievement(achievementId, out pbAchieved))
		{
			DebugLog.AchievementLog("업적 조회 실패: " + achievementId);
		}
		else if (!pbAchieved)
		{
			if (!SteamUserStats.SetAchievement(achievementId))
			{
				DebugLog.AchievementLog("업적 설정 실패: " + achievementId);
			}
			else if (!SteamUserStats.StoreStats())
			{
				DebugLog.AchievementLog("StoreStats 실패: " + achievementId);
			}
			else
			{
				DebugLog.AchievementLog("업적 해제 완료: " + achievementId);
			}
		}
	}
}
