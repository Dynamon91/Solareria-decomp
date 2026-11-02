using System.Collections.Generic;

namespace SPR.UI.Tutorial;

public static class ActionNameUtil
{
	public static readonly List<ActionName> ActionNameHasKeyGuide = new List<ActionName>
	{
		ActionName.Heal,
		ActionName.Roll,
		ActionName.Guard,
		ActionName.Parry,
		ActionName.Execution,
		ActionName.ParryAction,
		ActionName.ChargeAttack,
		ActionName.Ignition,
		ActionName.HyperBurst,
		ActionName.HyperPyronAction,
		ActionName.CoreStoneTapChange,
		ActionName.Teleport,
		ActionName.SpecialParry
	};

	public static readonly List<(ActionName former, ActionName latter)> ActionNameHasPage = new List<(ActionName, ActionName)>
	{
		(ActionName.Guard, ActionName.Parry),
		(ActionName.Groggy, ActionName.Execution)
	};

	public static readonly List<ActionName> ActionNameShowsImage = new List<ActionName>
	{
		ActionName.Level,
		ActionName.Part,
		ActionName.Heat,
		ActionName.FirstDeath,
		ActionName.FastTravel,
		ActionName.BaranWarningForDemo,
		ActionName.GuardiansOfTerradinForDemo,
		ActionName.Food
	};

	public static readonly List<ActionName> ActionNamesOnlyShowMessage = new List<ActionName>
	{
		ActionName.Camera,
		ActionName.ChargeAttack,
		ActionName.Map,
		ActionName.Roll
	};

	public static readonly List<ActionName> ActionNameAlsoShowsMessage = new List<ActionName>
	{
		ActionName.EnemyGuard,
		ActionName.Part,
		ActionName.Food,
		ActionName.Red
	};

	public static readonly List<(ActionName former, ActionName latter)> ActionNameAlsoShowsDifferentMessage = new List<(ActionName, ActionName)> { (ActionName.Parry, ActionName.Guard) };

	public static readonly List<ActionName> ActionNamesPopUp = new List<ActionName>
	{
		ActionName.Guard,
		ActionName.Parry,
		ActionName.Heat,
		ActionName.Ignition,
		ActionName.Bonfire,
		ActionName.FastTravel,
		ActionName.Roll,
		ActionName.Level,
		ActionName.Red,
		ActionName.Part,
		ActionName.ParryAction,
		ActionName.EnemyGuard,
		ActionName.Groggy,
		ActionName.Execution,
		ActionName.Sprout,
		ActionName.SproutReward,
		ActionName.Card,
		ActionName.CoreStoneTapChange,
		ActionName.FirstDeath,
		ActionName.Teleport,
		ActionName.Food,
		ActionName.HyperBurst,
		ActionName.HyperPyronAction,
		ActionName.LastEmber,
		ActionName.HeatHaze,
		ActionName.SpecialParry,
		ActionName.Counter,
		ActionName.DashPassWall,
		ActionName.Explosion,
		ActionName.WetStatusEffect,
		ActionName.SporeStatusEffect,
		ActionName.ShockStatusEffect,
		ActionName.DarknessEffect
	};

	public static readonly List<ActionName> ActionNameAtBonfire = new List<ActionName>
	{
		ActionName.Bonfire,
		ActionName.Teleport
	};

	private static readonly Dictionary<ActionName, int> ClearNum = new Dictionary<ActionName, int>
	{
		{
			ActionName.ChargeAttack,
			2
		},
		{
			ActionName.Execution,
			3
		},
		{
			ActionName.Heal,
			3
		},
		{
			ActionName.ParryAction,
			3
		},
		{
			ActionName.WetStatusEffect,
			3
		}
	};

	public static int GetClearNum(ActionName actionName)
	{
		return ClearNum.GetValueOrDefault(actionName, 1);
	}
}
