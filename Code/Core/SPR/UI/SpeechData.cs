using SPR.Library;

namespace SPR.UI;

public struct SpeechData
{
	public readonly string ScenarioKey;

	public readonly int Phase;

	public readonly Timer Timer;

	public SpeechData(string scenarioKey, int phase, float timing)
	{
		ScenarioKey = scenarioKey;
		Phase = phase;
		Timer = new Timer(timing);
	}
}
