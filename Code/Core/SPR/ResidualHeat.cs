using UnityEngine;

namespace SPR;

public class ResidualHeat
{
	private int currentValue;

	private bool blockHeal;

	public bool Active { get; private set; }

	public int Value
	{
		get
		{
			return currentValue;
		}
		private set
		{
			Game.Instance.Player.PlayerPart.OnResidualHeatChanged(value);
			currentValue = value;
		}
	}

	public float HealthPerValue { get; set; } = 1f;

	private static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	private static float Rate => PlayerStat.GetStat<ResidualRate>().Rate;

	private static int HealAmount => (int)PlayerStat.GetStat<ResidualHeal>().Current;

	public void Activate()
	{
		Active = true;
	}

	public void Deactivate()
	{
		if (Active)
		{
			RemoveResidualHeat(Mathf.CeilToInt(Value));
		}
		Active = false;
	}

	public void AddAmount(int amount)
	{
		if (Active)
		{
			Value += amount;
			ClampResidualHeat();
			PlayerStat.GetStat<Health>().UpdateHealthBar();
		}
	}

	public void RemoveResidualHeat(int amount)
	{
		if (Active)
		{
			Value -= amount;
			if (Value < 0)
			{
				Value = 0;
			}
			PlayerStat.GetStat<Health>().UpdateHealthBar();
		}
	}

	public void ClampResidualHeat()
	{
		if (Active)
		{
			Value = Mathf.Min(Value, PlayerStat.GetStat<Health>().MaxHealth - PlayerStat.GetStat<Health>().CurrentHealth);
		}
	}

	public void ChangeDamageToHeat(float damage)
	{
		if (Active)
		{
			Health stat = PlayerStat.GetStat<Health>();
			float num = Mathf.Clamp(Rate * damage, 1f, (float)stat.MaxHealth - ((float)stat.CurrentHealth - damage));
			AddAmount((int)num);
		}
	}

	public void Heal(int amount, float rate = 1f)
	{
		amount = Mathf.Min(amount, Value);
		RemoveResidualHeat(amount);
		int amount2 = Mathf.RoundToInt((float)amount * rate);
		PlayerStat.GetStat<Health>().AddHealth(amount2, healInstantly: true);
	}

	public void HealOnParry()
	{
		if (Active && !blockHeal)
		{
			int healthOnParry = GetHealthOnParry();
			if (healthOnParry > 0)
			{
				RemoveResidualHeat(healthOnParry);
				PlayerStat.GetStat<Health>().AddHealth((int)((float)healthOnParry * HealthPerValue), healInstantly: true);
			}
		}
	}

	public void BlockHealOnParry(bool block)
	{
		if (!Active)
		{
			block = false;
		}
		blockHeal = block;
	}

	private int GetHealthOnParry()
	{
		if (!Active)
		{
			return 0;
		}
		if (blockHeal)
		{
			return 0;
		}
		if (Value > 0)
		{
			return Mathf.Min(HealAmount, Value);
		}
		return 0;
	}
}
