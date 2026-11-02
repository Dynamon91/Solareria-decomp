using System;
using System.Collections.Generic;
using SPR.UI.Talk;

namespace SPR;

public class JumpScript : Script
{
	private readonly string triggerID;

	private readonly string scenarioID;

	private Scenario targetScenario;

	public override bool IsInstant => false;

	private Scenario TargetScenario => targetScenario ?? (targetScenario = ScenarioHelper.GetScenario(scenarioID));

	public JumpScript(IReadOnlyList<string> values)
	{
		if (!ScenarioHelper.IsScenarioExist(values[0]))
		{
			throw new Exception(values[0] + "는 존재하지 않는 ScenarioID입니다.");
		}
		if (!Trigger.IsTriggerExist(values[1]))
		{
			throw new Exception(values[1] + "는 존재하지 않는 TriggerID입니다. \"" + values[0] + "\" 시나리오를 확인하세요.");
		}
		scenarioID = values[0];
		triggerID = values[1];
	}

	public override void Run(TalkCanvas talkCanvas)
	{
		if (Trigger.CheckTrigger(triggerID))
		{
			talkCanvas.Jump(TargetScenario);
		}
		else
		{
			talkCanvas.RunNextScript();
		}
	}

	public override void Skip(TalkCanvas talkCanvas)
	{
		if (Trigger.CheckTrigger(triggerID))
		{
			talkCanvas.Jump_Skip(TargetScenario);
		}
		else
		{
			talkCanvas.RunNextScript_Skip();
		}
	}
}
