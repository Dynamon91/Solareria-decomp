namespace SPR.Achievement;

public class AchievementEntry
{
	private const string TriggerPrefix = "Trigger_Ach_";

	public string ID { get; }

	public bool IsAchieved { get; private set; }

	public string TriggerId => "Trigger_Ach_" + ID;

	public AchievementEntry(string achivementID)
	{
		ID = achivementID;
		IsAchieved = false;
	}

	public void SetAchieved(bool achieved)
	{
		IsAchieved = achieved;
	}
}
