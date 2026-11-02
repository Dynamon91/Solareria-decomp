using System;

namespace SPR;

public class VisitAreaTrigger : Trigger
{
	public VisitAreaTrigger(string target, string value)
	{
		if (target != "")
		{
			throw new FormatException("VisitArea Trigger에서 Target은 비어있어야합니다. 현재 값 : " + target);
		}
		BaseNode = new VisitAreaNode();
		ParseValue(value);
	}

	public override bool CheckTrigger()
	{
		return BaseNode.Result;
	}
}
