namespace SPR.UI.Upgrade;

public class UpgradeWarmhornCountModel : UpgradeModel
{
	public int WarmhornCount { get; }

	public UpgradeWarmhornCountModel(int currentCount)
	{
		WarmhornCount = currentCount;
	}
}
