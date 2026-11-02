using System;

namespace SPR;

public class TriggerTrigger : Trigger
{
	public TriggerTrigger(string target, string value)
	{
		if (target != "")
		{
			throw new FormatException("Trigger Trigger에서 Target은 비어있어야합니다. 현재 값 : " + target);
		}
		BaseNode = new TriggerNode();
		ParseValue(value);
	}

	public override bool CheckTrigger()
	{
		return BaseNode.Result;
	}
}
