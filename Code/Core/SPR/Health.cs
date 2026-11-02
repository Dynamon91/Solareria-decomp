using StaticData;
using UnityEngine;

namespace SPR;

public class Health : Stat
{
	private float darkness;

	public int MaxHealthWithDark => Mathf.CeilToInt(base.Max) - Mathf.RoundToInt(Darkness);

	public int MaxHealth => Mathf.CeilToInt(base.Max);

	public int CurrentHealth => Mathf.CeilToInt(base.Current);

	protected override bool UsingDecimal => false;

	public float Darkness
	{
		get
		{
			return darkness;
		}
		set
		{
			float num = value - darkness;
			if (num != 0f)
			{
				darkness = value;
				if (num > 0f)
				{
					AddHealth(-Mathf.RoundToInt(num));
				}
				bool instant = value == 0f;
				UpdateHealthBar(instant);
				Game.Instance.Player.mover.PlayerShield.UpdateShield();
			}
		}
	}

	public Health(float initial, Difficulty difficulty)
		: base(StatType.Warmth, initial, difficulty)
	{
	}

	public override float GetCurrentValue()
	{
		return MaxHealth;
	}

	public override float GetPreviewValue(float traitIncreases)
	{
		return Mathf.CeilToInt(base.GetPreviewValue(traitIncreases));
	}

	public void AddMax()
	{
		AddHealth(MaxHealth);
	}

	public void AddHealth(int amount, bool healInstantly = false)
	{
		if (amount > 0)
		{
			amount = Mathf.Min(amount, MaxHealthWithDark - CurrentHealth);
		}
		ChangeAmount(amount);
		Stat.PlayerStat.ResidualHeat.ClampResidualHeat();
		UpdateHealthBar();
		Game.Instance.Player.PlayerPart.OnHealthChanged();
		if (healInstantly)
		{
			Stat.FieldCanvas.PlayerStatusBoard.PlayHealTween(loop: false, GetType());
		}
	}

	public void UpdateHealthBar(bool instant = false)
	{
		Stat.FieldCanvas.PlayerStatusBoard.UpdateHealthBar(CurrentHealth, MaxHealthWithDark, MaxHealth, instant);
	}

	protected override void OnMaxChanged()
	{
	}

	private void UpdateMaxHealth()
	{
		UpdateHealthBar();
		Game.Instance.Player.mover.PlayerShield.UpdateShield();
	}
}
