using System.Collections.Generic;
using SPR.UI.Talk;

namespace SPR;

public class ChoiceDoNotRemove : ChoiceScript
{
	public ChoiceDoNotRemove(IReadOnlyList<string> values)
		: base(values)
	{
	}

	protected override void AddChoiceOption(TalkCanvas talkCanvas)
	{
		talkCanvas.AddChoiceOption(ChosenScenarioID, ChoiceText, NotiCheckType.None, removeInstantly: false);
	}
}
