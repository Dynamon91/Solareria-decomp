using System.Collections.Generic;
using Library;
using SPR.UI.Talk;

namespace SPR;

public class ChoiceScript : Script
{
	protected readonly string ChosenScenarioID;

	protected readonly string ChoiceText;

	protected readonly string TriggerID;

	public override bool IsInstant => false;

	public ChoiceScript(IReadOnlyList<string> values)
	{
		ChosenScenarioID = values[0];
		ChoiceText = values[Script.SpeechIndex].EnteredString();
		TriggerID = values[1];
	}

	public override void Run(TalkCanvas talkCanvas)
	{
		if (TriggerID.IsNullOrEmpty() || (Trigger.IsTriggerExist(TriggerID) && Trigger.CheckTrigger(TriggerID)))
		{
			AddChoiceOption(talkCanvas);
		}
		talkCanvas.HandleNextChoice();
	}

	public override void Skip(TalkCanvas talkCanvas)
	{
		if (TriggerID.IsNullOrEmpty() || (Trigger.IsTriggerExist(TriggerID) && Trigger.CheckTrigger(TriggerID)))
		{
			talkCanvas.AddChoiceOption_Skip(ChosenScenarioID, ChoiceText);
		}
		talkCanvas.HandleNextChoice_Skip();
	}

	protected virtual void AddChoiceOption(TalkCanvas talkCanvas)
	{
		talkCanvas.AddChoiceOption(ChosenScenarioID, ChoiceText, NotiCheckType.None, removeInstantly: true);
	}
}
