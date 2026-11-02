using System.Collections.Generic;

namespace SPR.Field;

public class EventStartEntranceElevator : EntranceElevator
{
	protected override List<int> GetDisableEnterStates()
	{
		List<int> disableEnterStates = base.GetDisableEnterStates();
		disableEnterStates.Add(4);
		return disableEnterStates;
	}
}
