using System.Collections.Generic;
using SPR.UI.Talk;

namespace SPR;

public class ChoiceNotiNewChat : ChoiceScript
{
	public ChoiceNotiNewChat(IReadOnlyList<string> values)
		: base(values)
	{
	}

	protected override void AddChoiceOption(TalkCanvas talkCanvas)
	{
		talkCanvas.AddChoiceOption(ChosenScenarioID, ChoiceText, NotiCheckType.NotiNewChat, removeInstantly: true);
	}
}
