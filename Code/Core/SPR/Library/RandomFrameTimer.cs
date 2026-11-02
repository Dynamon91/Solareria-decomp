using Library;
using UnityEngine;

namespace SPR.Library;

public class RandomFrameTimer
{
	private int min;

	private int max;

	private int passedFrame;

	private int targetFrame;

	public float NormalizedTime => (float)passedFrame / (float)targetFrame;

	public bool IsDone => passedFrame >= targetFrame;

	public RandomFrameTimer(int minInclusive, int maxInclusive)
	{
		min = minInclusive;
		max = maxInclusive + 1;
		Reset();
	}

	public void Reset()
	{
		passedFrame = 0;
		targetFrame = CryptoRandom.Range(min, max);
	}

	public void Tick()
	{
		if (Time.timeScale != 0f)
		{
			passedFrame = Mathf.Min(passedFrame + 1, targetFrame);
		}
	}
}
