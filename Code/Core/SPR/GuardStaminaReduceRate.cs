using StaticData;
using UnityEngine;

namespace SPR;

public class GuardStaminaReduceRate : Stat
{
	private const float Power = 2.4f;

	private const float Coefficient = 100f;

	private const float DenominatorNum = 100f;

	public float StaminaRate => 1f - GetStaminaReduceRate(base.Max) / 100f;

	protected override bool UsingPercent => true;

	public GuardStaminaReduceRate(float initial, Difficulty difficulty)
		: base(StatType.GuardStamina, initial, difficulty)
	{
	}

	public override float GetCurrentValue()
	{
		return GetStaminaReduceRate(base.GetCurrentValue());
	}

	public override float GetPreviewValue(float traitIncreases)
	{
		return GetStaminaReduceRate(base.GetPreviewValue(traitIncreases));
	}

	private static float GetStaminaReduceRate(float statValue)
	{
		return Mathf.Floor(100f * Mathf.Pow(statValue, 2.4f) / (Mathf.Pow(100f, 2.4f) + Mathf.Pow(statValue, 2.4f)));
	}
}
