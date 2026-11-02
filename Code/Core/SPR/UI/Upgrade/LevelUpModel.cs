using UnityEngine;

namespace SPR.UI.Upgrade;

public class LevelUpModel : UpgradeModel
{
	public int TargetLevel { get; private set; }

	public bool? ShouldIncrease { get; private set; }

	private static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	public int LevelDelta => TargetLevel - PlayerStat.Level;

	public LevelUpModel(int targetLevel, bool? shouldIncrease = null)
	{
		TargetLevel = Mathf.Min(targetLevel, 120);
		ShouldIncrease = shouldIncrease;
	}
}
