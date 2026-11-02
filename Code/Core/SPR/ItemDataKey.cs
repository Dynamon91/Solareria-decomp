using System.Collections.Generic;

namespace SPR;

public static class ItemDataKey
{
	public const int HeatCoreStoneID = 4;

	public const int DoubleJumpCard = 1010;

	public const string Inflare = "Inflare";

	public const string PyronWristband = "PyronWristband";

	public const string EnhancedWristband = "EnhancedWristband";

	public const string OphilinTreasureHint = "OphilinTreasureHint";

	public const string OphilinTreasureKey = "OphilinTreasureKey";

	public const string CardamineLetter = "CardamineLetter";

	public const string RoughFlowerPendant = "RoughFlowerPendant";

	public const string OldDiaryThird = "OldDiaryThird";

	public const string UnknownItem = "UnknownIngredient";

	public const string HealthPart = "Health";

	public const string ResidualHeat = "ResidualHeat";

	public const string Counter = "Counter";

	public const string HeatCoreStone = "Heat";

	public const string DashPassWall = "DashPassWall";

	public const string JumpDashAttack = "JumpDashAttack";

	public const string DoubleJumpCoreStone = "DoubleJump";

	public const string DownRollingCoreStone = "DownRolling";

	public const string FlameCore = "FlameCore";

	public const string SealTyche = "SealTyche";

	public const string SealChasa = "SealChasa";

	public const string CorDyingInfoID = "Earthford_28_Cor";

	public const string VeritaWarningInfoID = "InformationVeritaWarning";

	public const string AshHillsFiveRelics = "AshHillsFiveRelics";

	public const string PlagueSproutPendant = "PlagueSproutPendant";

	public const string CookingLog = "CookingLog";

	public const string GuardianStone = "GuardianStone";

	public const string MucoraPortalDoorKey = "MucoraPortalDoorKey";

	public const string SolinLabEntranceKey = "SolinLabEntranceKey";

	public const string RoyalSealNormal = "RoyalSealNormal";

	public const string RoyalSealPremium = "RoyalSealPremium";

	public const string SupremeSolinGem = "SupremeSolinGem";

	public const string SuperiorSolinMass = "SuperiorSolinMass";

	public const string EmptyWarmhornShell = "EmptyWarmhornShell";

	public const string MuhaLetter = "MuhaLetter";

	public const string SoowooCavePass = "SoowooCavePass";

	public const string BaranSpirit = "BaranSpirit";

	public const string DarkChasaSpirit = "DarkChasaSpirit";

	public const string GaiaSpirit = "GaiaSpirit";

	public const string MoldinSpirit = "MoldinSpirit";

	public const string SoowooSpirit = "SoowooSpirit";

	public static List<string> RelicSpirits => new List<string> { "PyronRelic", "OphilinRelic", "TerradinRelic", "ZephironRelic", "MucoraRelic", "BaranSpirit", "DarkChasaSpirit", "GaiaSpirit", "MoldinSpirit", "SoowooSpirit" };

	public static string SealItemKey(string bossName)
	{
		return "Seal" + bossName;
	}
}
