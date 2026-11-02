using System.Collections.Generic;
using SPR.UI.Talk;

namespace SPR;

public class ChoiceNotiForced : ChoiceScript
{
	public ChoiceNotiForced(IReadOnlyList<string> values)
		: base(values)
	{
	}

	protected override void AddChoiceOption(TalkCanvas talkCanvas)
	{
		talkCanvas.AddChoiceOption(ChosenScenarioID, ChoiceText, NotiCheckType.NotiForced, removeInstantly: true);
	}
}
