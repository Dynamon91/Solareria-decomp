namespace SPR.Library;

public class Timer
{
	public float Duration { get; protected set; }

	public float TimePassed { get; protected set; }

	public bool IsDone => TimePassed >= Duration;

	public Timer(float duration)
	{
		Duration = duration;
		TimePassed = 0f;
	}

	public virtual void Reset()
	{
		TimePassed = 0f;
	}

	public void Tick(float deltaTime)
	{
		TimePassed += deltaTime;
	}
}
