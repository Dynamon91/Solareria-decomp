using System;

namespace SPR;

public class WatchedCutsceneTrigger : Trigger
{
	public WatchedCutsceneTrigger(string target, string value)
	{
		if (target != "")
		{
			throw new FormatException("TalkTrigger에서 Target은 비어있어야합니다. 현재 값 : " + target);
		}
		BaseNode = new WatchedCutsceneNode();
		ParseValue(value);
	}

	public override bool CheckTrigger()
	{
		return BaseNode.Result;
	}
}
