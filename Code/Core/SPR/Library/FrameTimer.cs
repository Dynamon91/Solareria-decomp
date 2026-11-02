using UnityEngine;

namespace SPR.Library;

public class FrameTimer
{
	public int TargetFrame { get; }

	public int PassedFrame { get; private set; }

	public float NormalizedTime => (float)PassedFrame / (float)TargetFrame;

	public bool IsRunning
	{
		get
		{
			if (PassedFrame >= 0)
			{
				return !IsDone;
			}
			return false;
		}
	}

	public bool IsDone => PassedFrame >= TargetFrame;

	public FrameTimer(int targetFrame)
	{
		TargetFrame = targetFrame;
		PassedFrame = 0;
	}

	public void Reset()
	{
		PassedFrame = -1;
	}

	public void Tick()
	{
		if (Time.timeScale != 0f && !IsDone)
		{
			if (PassedFrame < 0)
			{
				PassedFrame = 0;
			}
			PassedFrame = Mathf.Min(PassedFrame + 1, TargetFrame);
		}
	}
}
