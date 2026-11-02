using System;

namespace SPR;

public class TalkTrigger : Trigger
{
	public TalkTrigger(string target, string value)
	{
		if (target != "")
		{
			throw new FormatException("TalkTrigger에서 Target은 비어있어야합니다. 현재 값 : " + target);
		}
		BaseNode = new TalkNode();
		ParseValue(value);
	}

	public override bool CheckTrigger()
	{
		return BaseNode.Result;
	}
}
