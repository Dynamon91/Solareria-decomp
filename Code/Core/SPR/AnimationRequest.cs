namespace SPR;

public struct AnimationRequest
{
	public readonly string Name;

	public readonly bool Loop;

	public readonly float TimeScale;

	public readonly float StartTime;

	public AnimationRequest(string name, bool loop, float timeScale, float startTime)
	{
		Name = name;
		Loop = loop;
		TimeScale = timeScale;
		StartTime = startTime;
	}
}
