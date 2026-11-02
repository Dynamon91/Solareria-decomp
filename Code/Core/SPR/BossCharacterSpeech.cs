using System.Collections.Generic;
using SPR.UI;

namespace SPR;

public class BossCharacterSpeech
{
	private readonly SpeechData[] speechData;

	private readonly BossCharacter boss;

	private int index;

	public bool IsDone { get; private set; }

	private NarrationBoard BossSpeech => Game.Instance.UIManager.FieldCanvas.BossBoard.BossSpeech;

	public BossCharacterSpeech(BossCharacter boss)
	{
		this.boss = boss;
		List<ScenarioData> list = DataManager.Instance.SearchAllScenarioDataContains("Fight_" + boss.Name);
		speechData = new SpeechData[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			string[] array = list[i].ID.Split('_');
			string s = array[^2].Replace("Phase", "");
			float timing = ((!array[^1].Contains("m")) ? float.Parse(array[^1].Replace("s", "")) : (float.Parse(array[^1].Split('m')[0]) * 60f + float.Parse(array[^1].Replace("s", ""))));
			speechData[i] = new SpeechData(list[i].ID, int.Parse(s), timing);
		}
	}

	public void Update(float deltaTime)
	{
		if (speechData.Length == 0 || speechData[index].Phase != boss.Phase)
		{
			return;
		}
		if (speechData[index].Timer.IsDone)
		{
			ScenarioData scenarioData = DataManager.Instance.GetScenarioData(speechData[index].ScenarioKey);
			BossSpeech.SetScenario(scenarioData);
			BossSpeech.SetVoice(scenarioData.Actor, scenarioData.VoiceKey);
			BossSpeech.SetActiveDuration();
			if (index == speechData.Length - 1)
			{
				IsDone = true;
				return;
			}
			index++;
			speechData[index].Timer.Tick(speechData[index - 1].Timer.Duration);
		}
		speechData[index].Timer.Tick(deltaTime);
	}

	public void PhaseChanged(int phase)
	{
		for (int i = 0; i < speechData.Length; i++)
		{
			if (speechData[i].Phase == phase)
			{
				index = i;
				break;
			}
		}
	}
}
