using System;
using System.Collections.Generic;
using SPR.L10N;
using UnityEngine;

namespace SPR;

public static class StatUtility
{
	public static List<(StatType statType, float increases)> GetTraitStatIncreasesByLevel(int id, int rank, int level)
	{
		TraitData traitData = DataManager.Instance.GetTraitData(id);
		List<(StatType, float)> list = new List<(StatType, float)>(traitData.Stats.Count);
		foreach (StatType stat in traitData.Stats)
		{
			float statIncrease = GetStatIncrease(traitData.Name, stat, rank, level);
			list.Add((stat, statIncrease));
		}
		return list;
	}

	private static float GetStatIncrease(string traitName, StatType statType, int rank, int level)
	{
		return statType switch
		{
			StatType.Warmth => GetHealthIncrease(rank, level), 
			StatType.Attack => GetAttackIncrease(rank, level), 
			StatType.Stamina => GetStaminaIncrease(level), 
			StatType.StaminaRegain => GetStaminaRegainIncrease(level), 
			StatType.Defense => (traitName == "Defense") ? GetDefenseOnDefenseIncrease(level) : GetDefaultDefenseIncrease(level), 
			StatType.CollectingPower => GetCollectingPowerIncrease(rank, level), 
			StatType.CollectingRange => GetCollectingPowerIncrease(rank, level), 
			StatType.GroggyAttack => GetGroggyAttackIncrease(level), 
			StatType.FireAttack => GetFireAttackIncrease(rank, level), 
			StatType.CounterAttack => GetCounterAttackIncrease(rank, level), 
			StatType.GuardStamina => (traitName == "Mystery") ? GetGuardStaminaOnMysteryIncrease(level) : GetGuardStaminaIncrease(level), 
			StatType.ShieldMaximum => GetShieldMaxIncrease(rank, level), 
			StatType.ResidualHeal => GetResidualHealOnHeatHazeIncrease(rank, level), 
			StatType.ResidualRate => 0f, 
			StatType.ParryGroggy => (traitName == "Defense") ? GetParryGroggyIncrease(level) : GetParryGroggyOnGuardIncrease(level), 
			StatType.GoodsProtection => 0f, 
			_ => throw new ArgumentOutOfRangeException("statType", statType, null), 
		};
	}

	private static float GetDefaultDefenseIncrease(int level)
	{
		return level;
	}

	private static float GetHealthIncrease(int rank, int level)
	{
		float f = 0f;
		switch (rank)
		{
		case 1:
			f = (float)(60 * level) / (float)(level + 20);
			break;
		case 2:
			f = (float)(80 * level) / (float)(level + 20);
			break;
		case 3:
			f = (float)(100 * level) / (float)(level + 20);
			break;
		}
		return Mathf.CeilToInt(f);
	}

	private static float GetShieldMaxIncrease(int rank, int level)
	{
		float result = 0f;
		switch (rank)
		{
		case 1:
			result = level;
			break;
		case 2:
			result = 1.5f * (float)level;
			break;
		case 3:
			result = 2f * (float)level;
			break;
		}
		return result;
	}

	private static float GetAttackIncrease(int rank, int level)
	{
		float f = 0f;
		switch (rank)
		{
		case 1:
			f = (float)(30 * level) / (float)(level + 20);
			break;
		case 2:
			f = (float)(45 * level) / (float)(level + 20);
			break;
		case 3:
			f = (float)(60 * level) / (float)(level + 20);
			break;
		}
		return Mathf.CeilToInt(f);
	}

	private static float GetGroggyAttackIncrease(int level)
	{
		return (float)level * 0.05f;
	}

	private static float GetStaminaIncrease(int level)
	{
		return level;
	}

	private static float GetStaminaRegainIncrease(int level)
	{
		return (float)level * 0.6f;
	}

	private static float GetGuardStaminaOnMysteryIncrease(int level)
	{
		return level;
	}

	private static float GetFireAttackIncrease(int rank, int level)
	{
		float f = 0f;
		switch (rank)
		{
		case 1:
			f = (float)(20 * level) / (float)(level + 20);
			break;
		case 2:
			f = (float)(30 * level) / (float)(level + 20);
			break;
		case 3:
			f = (float)(40 * level) / (float)(level + 20);
			break;
		}
		return Mathf.CeilToInt(f);
	}

	private static float GetDefenseOnDefenseIncrease(int level)
	{
		return level * 3;
	}

	private static float GetParryGroggyIncrease(int level)
	{
		return 0.01f * (float)level;
	}

	private static float GetResidualHealOnHeatHazeIncrease(int rank, int level)
	{
		return 0.5f * (float)level;
	}

	private static float GetCounterAttackIncrease(int rank, int level)
	{
		float result = 0f;
		switch (rank)
		{
		case 1:
			result = 0.5f * (float)level;
			break;
		case 2:
			result = level;
			break;
		case 3:
			result = 1.5f * (float)level;
			break;
		}
		return result;
	}

	private static float GetGuardStaminaIncrease(int level)
	{
		return level * 2;
	}

	private static float GetParryGroggyOnGuardIncrease(int level)
	{
		return 0.02f * (float)level;
	}

	private static float GetCollectingPowerIncrease(int rank, int level)
	{
		float result = 0f;
		switch (rank)
		{
		case 1:
			result = level * 2;
			break;
		case 2:
			result = level * 4;
			break;
		case 3:
			result = level * 6;
			break;
		}
		return result;
	}

	public static string GetStatName(StatType statType)
	{
		return Locale.GetLocaleString(LocaleKey.StatName(statType), Language.Korean);
	}
}
