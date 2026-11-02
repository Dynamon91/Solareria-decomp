using System;
using Library;

namespace SPR;

public class TraitTrigger : Trigger
{
	private readonly int traitID;

	private readonly int targetLevel;

	public TraitTrigger(string target, string value)
	{
		if (target == "")
		{
			throw new FormatException("Trait Trigger에서 해당 특성의 ID를 target값으로 반드시 사용해야 합니다!");
		}
		traitID = target.ToInt();
		targetLevel = ((!(value == "")) ? value.ToInt() : 0);
	}

	public override bool CheckTrigger()
	{
		return Trigger.SaveData.PlayerData.GetCardLevel(traitID) >= targetLevel;
	}
}
