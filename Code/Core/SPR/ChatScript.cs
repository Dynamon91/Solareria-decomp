using System;
using System.Collections.Generic;
using SPR.UI.Talk;

namespace SPR;

public class ChatScript : Script
{
	private readonly string scenarioID;

	private readonly string triggerID;

	public override bool IsInstant => false;

	public bool IsTriggerSatisfy => Trigger.CheckTrigger(triggerID);

	public bool IsAlreadyWatched => Game.Instance.SaveData.TalkData.GetTalkCount(scenarioID) > 0;

	public bool IsHasTrigger => !string.IsNullOrEmpty(triggerID);

	public Scenario Scenario => ScenarioHelper.GetScenario(scenarioID);

	public string ScenarioID => scenarioID;

	public ChatScript(IReadOnlyList<string> values)
	{
		scenarioID = values[0];
		triggerID = values[1];
		if (!ScenarioHelper.IsScenarioExist(scenarioID))
		{
			throw new Exception(scenarioID + "는 존재하지 않는 ScenarioID입니다.");
		}
	}

	public override void Run(TalkCanvas talkCanvas)
	{
		talkCanvas.Chat();
	}

	public override void Skip(TalkCanvas talkCanvas)
	{
		talkCanvas.Chat_Skip();
	}
}
