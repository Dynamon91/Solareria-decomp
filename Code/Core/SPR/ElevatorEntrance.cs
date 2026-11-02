using Cysharp.Threading.Tasks;
using SPR.Field;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class ElevatorEntrance : Entrance
{
	private EntranceElevator elevator;

	private bool isLoading;

	private EntranceElevator Elevator => elevator ?? (elevator = FindEntranceElevator());

	protected override bool CanConnectMap => true;

	private void Awake()
	{
		isLoading = false;
	}

	protected override void FixedUpdate()
	{
		if (!isLoading && !Game.Instance.Player.IsInSceneLoadSequence && (!Elevator.IsMoving || !TryLoadField(EntranceDirection)) && !Elevator.IsPlayerRidden() && PhysicsUtility.IsPlayerStay((Vector2)base.transform.position + base.BoxCollider.offset, base.BoxCollider.size))
		{
			isLoading = true;
			Elevator.SetColliderActive(value: false);
			Game.Instance.LoadFieldAsync(SceneDataModel.MoveModel(TargetSceneID, base.name)).Forget();
		}
	}

	private bool TryLoadField(Direction entranceDirection)
	{
		switch (entranceDirection)
		{
		case Direction.Up:
			if (Elevator.EndState != SPR.Field.Elevator.State.DownOrLeftFloor)
			{
				return false;
			}
			if (PhysicsUtility.IsPlayerStay(base.transform.position, base.BoxCollider.size))
			{
				return TryLoadField_Internal(checkRiding: false);
			}
			return false;
		case Direction.Down:
			if (Elevator.EndState != SPR.Field.Elevator.State.UpOrRightFloor)
			{
				return false;
			}
			if (Elevator.UnderBounds.center.y >= base.BoxCollider.bounds.center.y)
			{
				return TryLoadField_Internal(checkRiding: true);
			}
			return false;
		default:
			return false;
		}
		bool TryLoadField_Internal(bool checkRiding)
		{
			if (checkRiding && !Elevator.IsPlayerRidden())
			{
				return false;
			}
			isLoading = true;
			LoadField();
			Elevator.SetSceneLoading();
			return true;
		}
	}

	public override async UniTask MovePlayerOnEnterAsync()
	{
		Initialize();
		if (EntranceDirection == Direction.Up)
		{
			Elevator.SetColliderActive(value: false);
		}
		await base.MovePlayerOnEnterAsync();
		if (EntranceDirection == Direction.Up)
		{
			Elevator.SetColliderActive(value: true);
		}
	}

	protected override SceneDataModel GetSceneDataModel()
	{
		return SceneDataModel.MoveByOtherModel(TargetSceneID, base.name, isVertical: true);
	}

	public Vector2 GetElevatorGroundPosition()
	{
		if (!(Elevator != null))
		{
			return Vector2.zero;
		}
		return Elevator.GetGroundPositionOnEnter();
	}

	private EntranceElevator FindEntranceElevator()
	{
		EntranceElevator componentInChildren = base.ConnectArea.GetComponentInChildren<EntranceElevator>();
		if (componentInChildren != null)
		{
			return componentInChildren;
		}
		DebugLog.GeneralLogError(base.name + " 통로 방향으로 불러올 수 있는 엘리베이터가 없습니다.");
		return null;
	}
}
