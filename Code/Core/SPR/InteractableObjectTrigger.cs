using System;

namespace SPR;

public class InteractableObjectTrigger : Trigger
{
	private readonly string objectID;

	private readonly bool shouldInteracted;

	public InteractableObjectTrigger(string target, string value)
	{
		if (target != "")
		{
			throw new FormatException("InteractableObject Trigger에서 Target은 비어있어야합니다. 현재 값 : " + target);
		}
		if (value[0] == '!')
		{
			shouldInteracted = false;
			objectID = value.Remove(0, 1);
		}
		else
		{
			shouldInteracted = true;
			objectID = value;
		}
	}

	public override bool CheckTrigger()
	{
		int state;
		return (Trigger.SaveData.WorldData.TryGetObjectState(objectID, out state) && state != 0) == shouldInteracted;
	}
}
