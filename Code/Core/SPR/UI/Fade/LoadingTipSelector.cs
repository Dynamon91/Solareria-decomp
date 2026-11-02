using System;
using System.Collections.Generic;
using Library;
using SPR.Save;
using SPR.UI.Tutorial;
using UnityEngine;

namespace SPR.UI.Fade;

public sealed class LoadingTipSelector
{
	private struct TipShowingRule
	{
		public readonly TipName Name;

		public readonly Func<bool> NormalCondition;

		public readonly Func<IAttackSource, bool> WeightedCondition;

		public readonly Func<bool> PrimaryCondition;

		public readonly int Weight;

		public TipShowingRule(TipName name)
		{
			Name = name;
			NormalCondition = () => true;
			WeightedCondition = null;
			PrimaryCondition = null;
			Weight = 1;
		}

		public TipShowingRule(TipName name, Func<bool> normalCondition)
		{
			Name = name;
			NormalCondition = normalCondition;
			WeightedCondition = null;
			PrimaryCondition = null;
			Weight = 1;
		}

		public TipShowingRule(TipName name, Func<bool> normalCondition, Func<IAttackSource, bool> weightedCondition, Func<bool> primaryCondition, int weight)
		{
			Name = name;
			NormalCondition = normalCondition;
			WeightedCondition = weightedCondition;
			PrimaryCondition = primaryCondition;
			Weight = Mathf.Max(1, weight);
		}
	}

	private readonly List<TipShowingRule> rules;

	private static SaveData SaveData => Game.Instance.SaveData;

	public LoadingTipSelector()
	{
		WanderingData wanderingData = Game.Instance.SaveData.WanderingData;
		rules = new List<TipShowingRule>
		{
			new TipShowingRule(TipName.LastAltar),
			new TipShowingRule(TipName.QuickMap),
			new TipShowingRule(TipName.HeatCore),
			new TipShowingRule(TipName.PyronAction, () => !wanderingData.ShouldShowTutorial(ActionName.ParryAction)),
			new TipShowingRule(TipName.Burst, () => !wanderingData.ShouldShowTutorial(ActionName.Execution)),
			new TipShowingRule(TipName.RageFlood, () => !wanderingData.ShouldShowTutorial(ActionName.Red)),
			new TipShowingRule(TipName.ShadowPlague, () => SaveData.EnemyFightData.IsBossCleared(BossName.Fidiel)),
			new TipShowingRule(TipName.Litt, () => SaveData.EnemyFightData.IsBossCleared(BossName.Fidiel)),
			new TipShowingRule(TipName.AshHillsLibrary, () => SaveData.WorldData.HasInteractedFieldObject("Earthford_03_Portal_Library_03")),
			new TipShowingRule(TipName.SolinDiscovery, () => SaveData.TalkData.GetInteractCount(ActorName.Verita) > 0),
			new TipShowingRule(TipName.LastEmber, () => PlayerHeat.CanActivateSkill(HeatSkillType.LastEmber)),
			new TipShowingRule(TipName.Breakfall, IncludeDownRollingInRandom, HasDiedToFidielTwiceOrLess, null, 99),
			new TipShowingRule(TipName.HyperPyronAction, null, null, () => Game.Instance.RuntimeData.GetTipCondition(HeatSkillType.HyperPyronAction), 100),
			new TipShowingRule(TipName.HyperBurst, null, null, () => Game.Instance.RuntimeData.GetTipCondition(HeatSkillType.HyperBurst), 101)
		};
	}

	public TipName GetNextTip(IAttackSource attackSource)
	{
		List<TipName> list = new List<TipName>();
		TipName tipName = TipName.None;
		int num = int.MinValue;
		foreach (TipShowingRule rule in rules)
		{
			if (SafeInvoke(rule.PrimaryCondition) && rule.Weight >= num)
			{
				tipName = rule.Name;
				num = rule.Weight;
			}
			if (tipName == TipName.None)
			{
				int num2 = 0;
				if (SafeInvoke(rule.WeightedCondition, attackSource))
				{
					num2 = rule.Weight;
				}
				else if (SafeInvoke(rule.NormalCondition))
				{
					num2 = 1;
				}
				for (int i = 0; i < num2; i++)
				{
					list.Add(rule.Name);
				}
			}
		}
		if (tipName != TipName.None)
		{
			return tipName;
		}
		if (list.Count == 0)
		{
			DebugLog.GeneralLogWarning("[TipSelector] 보여줄 팁이 없습니다. 그럴 수가 있나...?");
			return TipName.None;
		}
		int index = CryptoRandom.Range(0, list.Count);
		return list[index];
	}

	private static bool SafeInvoke(Func<bool> fn)
	{
		if (fn == null)
		{
			return false;
		}
		try
		{
			return fn();
		}
		catch (Exception arg)
		{
			DebugLog.GeneralLogError($"[TipSelector] Predicate threw exception: {arg}");
			return false;
		}
	}

	private static bool SafeInvoke(Func<IAttackSource, bool> fn, IAttackSource attackSource)
	{
		if (fn == null)
		{
			return false;
		}
		try
		{
			return fn(attackSource);
		}
		catch (Exception arg)
		{
			DebugLog.GeneralLogError($"[TipSelector] Predicate threw exception: {arg}");
			return false;
		}
	}

	private bool HasDiedToFidielTwiceOrLess(IAttackSource attackSource)
	{
		bool num = Game.Instance.CurrentFieldID == new FieldID(SectorName.Earthford, 15);
		int bossTryCount = SaveData.EnemyFightData.GetBossTryCount(BossName.Fidiel);
		if (num)
		{
			return bossTryCount <= 2;
		}
		return false;
	}

	private bool IncludeDownRollingInRandom()
	{
		int bossTryCount = SaveData.EnemyFightData.GetBossTryCount(BossName.Fidiel);
		bool flag = SaveData.PlayerData.HasCoreStone("DownRolling");
		if (bossTryCount > 0)
		{
			return !flag;
		}
		return false;
	}
}
