using UnityEngine;

namespace SPR.Field;

public class SolinLabHorizontalElevator : HorizontalElevator
{
	protected override void OnCompleteMoving()
	{
		if (Mathf.Approximately(base.CurrentEndPos, leftPosition.x))
		{
			leftFloorLever.ChangeState(1);
		}
		base.OnCompleteMoving();
	}
}
