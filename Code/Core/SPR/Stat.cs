using System.Globalization;
using SPR.UI.Field;
using StaticData;
using UnityEngine;

namespace SPR;

public class Stat
{
	private const int Scale = 1000;

	public StatType StatType { get; }

	public float Initial { get; private set; }

	public float DifficultyRate { get; private set; }

	public float Trait { get; private set; }

	public float TraitPassive { get; private set; }

	public float TraitPassiveRate { get; private set; }

	public float Food { get; private set; }

	public float PartValue { get; private set; }

	public float PartRate { get; private set; }

	public float Other { get; private set; }

	public float Max => (StatRate_scaled * (Initial + Trait + TraitPassive + Food + PartValue) + Other) / 1000f;

	public float Current { get; private set; }

	public bool IsFull => Current >= Max;

	protected virtual bool UsingPercent => false;

	protected virtual bool UsingDecimal => true;

	private float StatRate_scaled => (TraitPassiveRate + PartRate + 1f) * DifficultyRate * 1000f;

	protected static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	protected static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	public Stat(StatType statType, float initial, Difficulty difficulty = Difficulty.None)
	{
		StatType = statType;
		Initial = initial;
		Trait = 0f;
		TraitPassive = 0f;
		TraitPassiveRate = 0f;
		Food = 0f;
		PartValue = 0f;
		PartRate = 0f;
		Other = 0f;
		DifficultyRate = Game.DataManager.GetCurrentDifficultyStat(statType.ToString(), difficulty);
		Current = Max;
	}

	public float GetPrintValue(float traitTotalIncrease = -1f)
	{
		float num = ((traitTotalIncrease < 0f) ? GetCurrentValue() : GetPreviewValue(traitTotalIncrease));
		if (!UsingDecimal)
		{
			return (int)num;
		}
		return (float)(int)(num * 10f) / 10f;
	}

	public virtual float GetCurrentValue()
	{
		return Max;
	}

	public virtual float GetPreviewValue(float traitIncreases)
	{
		return (StatRate_scaled * (Initial + traitIncreases + TraitPassive + Food + PartValue) + Other) / 1000f;
	}

	protected void AddInitial(float amount)
	{
		Initial += amount;
		AddCurrentOnMaxChanged(amount);
		OnAddInitial(Initial);
	}

	public void ResetTrait()
	{
		Trait = 0f;
	}

	public void AddTraitStat(float value)
	{
		Trait += value;
		AddCurrentOnMaxChanged(value);
		OnMaxChanged();
	}

	public void ResetTraitPassive()
	{
		TraitPassive = 0f;
		TraitPassiveRate = 0f;
	}

	public void AddTraitPassive(float value)
	{
		TraitPassive += value;
		if (TraitPassive < 0f)
		{
			TraitPassive = 0f;
		}
		AddCurrentOnMaxChanged(value);
		OnMaxChanged();
	}

	public void AddTraitPassiveRate(float value)
	{
		TraitPassiveRate += value;
		if (TraitPassiveRate < 0f)
		{
			TraitPassiveRate = 0f;
		}
		AddCurrentOnMaxChanged(value);
		OnMaxChanged();
	}

	public void ResetFood()
	{
		Food = 0f;
	}

	public void AddFoodStat(float value)
	{
		Food += value;
		AddCurrentOnMaxChanged(value);
		OnMaxChanged();
	}

	public void ResetPart()
	{
		PartValue = 0f;
		PartRate = 0f;
	}

	public void AddPartValue(float value)
	{
		PartValue += value;
		AddCurrentOnMaxChanged(value);
		OnMaxChanged();
	}

	public void AddPartRate(float value)
	{
		float partRate = PartRate;
		PartRate += value;
		Current /= 1f + partRate;
		Current *= 1f + PartRate;
		if (Current > Max)
		{
			Current = Max;
		}
		if (Current < 0f)
		{
			Current = 0f;
		}
		OnMaxChanged();
	}

	public void AddOther(float value)
	{
		Other += value;
		ChangeAmount(value);
	}

	public void ChangeDifficulty(Difficulty difficulty)
	{
		float difficultyRate = DifficultyRate;
		DifficultyRate = Game.DataManager.GetCurrentDifficultyStat(StatType.ToString(), difficulty);
		Current /= difficultyRate;
		Current *= DifficultyRate;
		if (Current > Max)
		{
			Current = Max;
		}
		if (Current < 0f)
		{
			Current = 0f;
		}
		OnMaxChanged();
	}

	public void ChangeAmount(float amount)
	{
		Current = Mathf.Clamp(Current + amount, 0f, Max);
	}

	public string GetPrintText(float traitTotalIncrease = -1f)
	{
		return GetPrintValue(traitTotalIncrease).ToString(CultureInfo.InvariantCulture) + (UsingPercent ? " %" : "");
	}

	protected virtual void OnMaxChanged()
	{
	}

	protected virtual void OnAddInitial(float currenInitial)
	{
	}

	private void AddCurrentOnMaxChanged(float delta)
	{
		float amount = delta * StatRate_scaled / 1000f;
		ChangeAmount(amount);
	}
}
