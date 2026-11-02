namespace SPR.Achievement;

public interface IAchievementProvider
{
	void Initialize();

	void UnlockAchievement(string achievementId);
}
