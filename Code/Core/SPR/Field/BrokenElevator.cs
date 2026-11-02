using Spine.Unity;
using UnityEngine;

namespace SPR.Field;

public class BrokenElevator : EntranceElevator
{
	[SerializeField]
	protected SkeletonAnimation elevatorSpine;

	protected override void LoadState_Internal(int state)
	{
		base.LoadState_Internal(state);
		if (state == 4)
		{
			LoadBrokenState();
		}
	}

	protected virtual void LoadBrokenState()
	{
		ChangeFloorState(State.DownOrLeftFloor);
		SetInitialPosition(State.DownOrLeftFloor);
	}

	protected override void OnCompleteMoving()
	{
		base.OnCompleteMoving();
		ChangeState(4);
		ChangeOppositeElevatorState(4);
	}

	public override void OnStartRide()
	{
		if (CurrentStateNum != 4)
		{
			base.OnStartRide();
		}
	}
}
