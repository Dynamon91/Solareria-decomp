using UnityEngine;

namespace SPR;

public struct ClampedInt
{
	private int value;

	public int Value
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

	public int Min { get; private set; }

	public int Max { get; private set; }

	public ClampedInt(int current, int min, int max)
	{
		this = default(ClampedInt);
		Min = min;
		Max = max;
		Value = current;
	}

	public static ClampedInt operator +(ClampedInt value, int amount)
	{
		value.Value += amount;
		return value;
	}

	public static ClampedInt operator -(ClampedInt value, int amount)
	{
		value.Value -= amount;
		return value;
	}
}
