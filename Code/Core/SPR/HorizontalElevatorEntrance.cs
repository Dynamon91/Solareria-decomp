using Cysharp.Threading.Tasks;
using SPR.Field;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class HorizontalElevatorEntrance : Entrance
{
	private HorizontalElevator elevator;

	private bool isLoading;

	private HorizontalElevator Elevator => elevator ?? (elevator = FindEntranceElevator());

	protected override bool CanConnectMap => true;

	private void Awake()
	{
		isLoading = false;
	}

	protected override void FixedUpdate()
	{
		if (!isLoading && PhysicsUtility.IsPlayerStay((Vector2)base.transform.position + base.BoxCollider.offset, base.BoxCollider.size))
		{
			if (Elevator.IsMoving && Elevator.IsPlayerRidden())
			{
				TryLoadField(EntranceDirection);
			}
			else if (Game.Instance.InputManager.IsCurrentControlTarget(Game.Instance.Player) || Game.Instance.InputManager.IsCurrentControlTarget(Game.Instance.DevController))
			{
				isLoading = true;
				Elevator.SetPreviousState();
				Game.Instance.LoadFieldAsync(SceneDataModel.MoveModel(TargetSceneID, base.name)).Forget();
			}
		}
	}

	private bool TryLoadField(Direction entranceDirection)
	{
		switch (entranceDirection)
		{
		case Direction.Right:
			if (Elevator.EndState != SPR.Field.Elevator.State.DownOrLeftFloor)
			{
				return false;
			}
			return TryLoadField_Internal(checkRiding: false);
		case Direction.Left:
			if (Elevator.EndState != SPR.Field.Elevator.State.UpOrRightFloor)
			{
				return false;
			}
			return TryLoadField_Internal(checkRiding: true);
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

	public override Vector2 GetPlayerSpawnPosition()
	{
		Initialize();
		Vector2 result = base.transform.position;
		switch (EntranceDirection)
		{
		case Direction.Right:
			result += new Vector2(-2f, 0f);
			break;
		case Direction.Left:
			result += new Vector2(2f, 0f);
			break;
		}
		return result;
	}

	public override Vector2 GetPlayerEnterEndPosition()
	{
		Initialize();
		Vector2 result = base.transform.position;
		switch (EntranceDirection)
		{
		case Direction.Right:
			result += new Vector2(2f, 0f);
			break;
		case Direction.Left:
			result += new Vector2(-2f, 0f);
			break;
		}
		return result;
	}

	public override UniTask MovePlayerOnEnterAsync()
	{
		return Game.Instance.Player.MoveXPositionAsync(GetPlayerEnterEndPosition().x, isRunning: true);
	}

	protected override SceneDataModel GetSceneDataModel()
	{
		return SceneDataModel.MoveByOtherModel(TargetSceneID, base.name, isVertical: false);
	}

	public Vector2 GetElevatorGroundPosition()
	{
		if (!(Elevator != null))
		{
			return Vector2.zero;
		}
		return Elevator.GetGroundPositionOnEnter();
	}

	private HorizontalElevator FindEntranceElevator()
	{
		HorizontalElevator componentInChildren = base.ConnectArea.GetComponentInChildren<HorizontalElevator>(includeInactive: true);
		if (componentInChildren != null)
		{
			return componentInChildren;
		}
		DebugLog.GeneralLogError(base.name + " 통로 방향으로 불러올 수 있는 엘리베이터가 없습니다.");
		return null;
	}
}
