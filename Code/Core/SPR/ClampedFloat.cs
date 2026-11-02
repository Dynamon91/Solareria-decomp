using UnityEngine;

namespace SPR;

public struct ClampedFloat
{
	private float value;

	public float Value
	{
		get
		{
			return value;
		}
		private set
		{
			this.value = Mathf.Clamp(value, Min, Max);
		}
	}

	public float Min { get; private set; }

	public float Max { get; private set; }

	public ClampedFloat(float current, float min, float max)
	{
		this = default(ClampedFloat);
		Min = min;
		Max = max;
		Value = current;
	}

	public static ClampedFloat operator +(ClampedFloat value, float amount)
	{
		value.Value += amount;
		return value;
	}

	public static ClampedFloat operator -(ClampedFloat value, float amount)
	{
		value.Value -= amount;
		return value;
	}
}
