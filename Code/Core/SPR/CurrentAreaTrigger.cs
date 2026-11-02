using System;

namespace SPR;

public class CurrentAreaTrigger : Trigger
{
	private readonly FieldID fieldID;

	private readonly bool shouldBeThere;

	public CurrentAreaTrigger(string target, string value)
	{
		if (target != "")
		{
			throw new FormatException("CurrentArea Trigger에서 Target은 비어있어야합니다. 현재 값 : " + target);
		}
		if (value[0] == '!')
		{
			shouldBeThere = false;
			fieldID = FieldID.ConvertToFieldID(value.Substring(1, value.Length - 1));
		}
		else
		{
			shouldBeThere = true;
			fieldID = FieldID.ConvertToFieldID(value);
		}
	}

	public override bool CheckTrigger()
	{
		return fieldID == Game.Instance.CurrentFieldID == shouldBeThere;
	}
}
