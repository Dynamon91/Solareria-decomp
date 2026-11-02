using System.Collections.Generic;
using SPR.UI.Map;
using SPR.UI.Talk;

namespace SPR;

public class ScenarioInfoScript : Script
{
	private readonly string infoStoneID;

	public override bool IsInstant => true;

	public ScenarioInfoScript(IReadOnlyList<string> values)
	{
		infoStoneID = values[Script.ScenarioKOIndex];
	}

	public override void Run(TalkCanvas talkCanvas)
	{
		MenuMapPanel.AddInfoStoneInformation(infoStoneID);
		talkCanvas.RunNextScript();
	}

	public override void Skip(TalkCanvas talkCanvas)
	{
		MenuMapPanel.AddInfoStoneInformation(infoStoneID);
		talkCanvas.RunNextScript_Skip();
	}
}
