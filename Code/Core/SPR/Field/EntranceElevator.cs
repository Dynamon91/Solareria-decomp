using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR.Field;

public class EntranceElevator : FieldElevator
{
	[Header("Field Setting")]
	[SerializeField]
	private FieldID upFieldID;

	[SerializeField]
	private FieldID downFieldID;

	private List<int> disableStatesOnEnter;

	private bool isSceneLoading;

	public State EndState => GetNextState(base.CurrentEndPos);

	public bool IsUpFloorElevator => CurrentFieldID == upFieldID;

	protected FieldID OppositeFieldID
	{
		get
		{
			if (!IsUpFloorElevator)
			{
				return upFieldID;
			}
			return downFieldID;
		}
	}

	public override string ID => $"Elevator_{CurrentFieldID}";

	protected string OppositeID => $"Elevator_{OppositeFieldID}";

	protected static FieldID CurrentFieldID => Game.Instance.CurrentFieldID;

	protected override void Awake()
	{
		base.Awake();
		disableStatesOnEnter = GetDisableEnterStates();
	}

	protected virtual List<int> GetDisableEnterStates()
	{
		return new List<int>();
	}

	protected override void LoadDefaultState()
	{
		State state = ((defaultState == State.UpOrRightFloor) ? State.UpOrRightFloor : State.DownOrLeftFloor);
		ChangeFloorState(state);
		SetInitialPosition(FloorState);
		SetColliderActive(value: true);
	}

	protected override void LoadMovedState()
	{
		State state = ((defaultState != State.UpOrRightFloor) ? State.UpOrRightFloor : State.DownOrLeftFloor);
		ChangeFloorState(state);
		SetInitialPosition(FloorState);
		SetColliderActive(base.MyArea.IsInArea(base.transform.position));
	}

	protected override void LoadState_Internal(int state)
	{
		base.LoadState_Internal(state);
		if (disableStatesOnEnter.Contains(state))
		{
			State state2 = ((defaultState != State.UpOrRightFloor) ? State.UpOrRightFloor : State.DownOrLeftFloor);
			ChangeFloorState(state2);
			SetInitialPosition(FloorState);
		}
	}

	protected void SetInitialPosition(State state)
	{
		float initialPosition;
		switch (state)
		{
		case State.Moving:
			return;
		case State.UpOrRightFloor:
			initialPosition = upFloorWorldPosY;
			break;
		case State.DownOrLeftFloor:
			initialPosition = downFloorWorldPosY;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		SetInitialPosition(initialPosition);
	}

	protected override void OnStartMoving()
	{
		base.OnStartMoving();
		SetColliderActive(value: true);
		int num = Mathf.Min(1, CurrentStateNum);
		ChangeOppositeElevatorState(num);
		ChangeState(num ^ 1);
	}

	protected override void OnCompleteMoving()
	{
		base.OnCompleteMoving();
		if (IsUpFloorElevator && FloorState == State.DownOrLeftFloor && !base.Bounds.Intersects(Elevator.PlayerMover.ColliderBounds))
		{
			Vector3 position = Elevator.PlayerMover.transform.position;
			int stateNum = ((IsPlayerOnCeiling() || (position.y < base.Bounds.max.y + size.y && position.x > base.Bounds.min.x && position.x < base.Bounds.max.x)) ? 3 : CurrentStateNum);
			ChangeOppositeElevatorState(stateNum);
		}
		else if ((IsUpFloorElevator && FloorState == State.UpOrRightFloor) || (!IsUpFloorElevator && FloorState == State.DownOrLeftFloor))
		{
			ChangeOppositeElevatorState(CurrentStateNum);
		}
	}

	public Vector2 GetGroundPositionOnEnter()
	{
		float num = (IsUpFloorElevator ? downFloorWorldPosY : upFloorWorldPosY);
		return new Vector2(base.transform.position.x, num - size.y / 2f + 1f + Elevator.PlayerMover.ColliderBounds.size.y / 2f);
	}

	public async UniTask MoveOnEnterAsync()
	{
		if (!base.IsActive)
		{
			return;
		}
		await UniTask.WaitUntil(() => base.IsLoaded);
		DeactivatePreviousPath();
		SetColliderActive(value: true);
		State state = ((!IsUpFloorElevator && !disableStatesOnEnter.Contains(CurrentStateNum)) ? State.UpOrRightFloor : State.DownOrLeftFloor);
		ChangeFloorState(state);
		if (CurrentStateNum == 3)
		{
			SetInitialPosition(upFloorWorldPosY - size.y);
		}
		else
		{
			SetInitialPosition(state);
		}
		if (!disableStatesOnEnter.Contains(CurrentStateNum))
		{
			OnEnter();
			float num = (IsUpFloorElevator ? upFloorWorldPosY : downFloorWorldPosY);
			float direction = Mathf.Sign(num - base.transform.position.y);
			float targetPosY = base.transform.position.y + direction * size.y * 1.25f;
			StartMove(num, ignoreWait: true);
			await UniTask.WaitUntil(() => targetPosY > base.transform.position.y != direction > 0f || !base.IsMoving);
		}
	}

	protected virtual void OnEnter()
	{
	}

	protected void ChangeOppositeElevatorState(int stateNum)
	{
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(OppositeFieldID.SectorName, out var data))
		{
			data.SaveObjectState(OppositeID, stateNum);
			DebugLog.GeneralLog($"{OppositeID} 반대편 EV 엘베 상태 : {stateNum}");
		}
	}

	protected override void StopMoveFXs()
	{
		StopMoveParticle();
		StopMoveSfx(!isSceneLoading);
	}

	public void SetSceneLoading()
	{
		isSceneLoading = true;
	}
}
