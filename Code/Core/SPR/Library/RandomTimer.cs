using UnityEngine;

namespace SPR.Library;

public class RandomTimer : Timer
{
	private readonly float min;

	private readonly float max;

	public RandomTimer(float min, float max)
		: base(max)
	{
		this.min = min;
		this.max = max;
	}

	public override void Reset()
	{
		base.TimePassed = 0f;
		base.Duration = Random.Range(min, max);
	}
}
