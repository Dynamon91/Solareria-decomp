using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;

namespace SPR.Field;

public class HorizontalElevator : Elevator
{
	[SerializeField]
	protected ElevatorLever rightFloorLever;

	[SerializeField]
	protected ElevatorLever leftFloorLever;

	[SerializeField]
	protected SpriteRenderer lightSpriteRenderer;

	[SerializeField]
	private Animator lightAnimator;

	[SerializeField]
	protected Vector2 leftPosition;

	[SerializeField]
	protected Vector2 rightPosition;

	[SerializeField]
	private bool isOnOpposite;

	[SerializeField]
	private List<GameObject> transparentWalls;

	private ElevatorTrigger elevatorTrigger;

	protected bool BlockComplete;

	private bool isRidden;

	private bool isCompleted;

	protected bool IsMovingWithFade = true;

	[SerializeField]
	private FieldID rightFieldID;

	[SerializeField]
	private FieldID leftFieldID;

	public State EndState => GetNextState(base.CurrentEndPos);

	public Bounds Bounds => new Bounds(base.transform.position, floorCollider.bounds.size + Vector3.up * Mathf.Abs(floorCollider.offset.y) * 2f);

	public override Bounds UnderBounds => new Bounds(floorCollider.bounds.center, floorCollider.bounds.size);

	public override bool IsVertical => false;

	private static FieldID CurrentFieldID => Game.Instance.CurrentFieldID;

	private ElevatorTrigger ElevatorTrigger => elevatorTrigger ?? (elevatorTrigger = GetComponentInChildren<ElevatorTrigger>());

	protected virtual bool IsEntranceElevator => true;

	private FieldID OppositeFieldID
	{
		get
		{
			if (!IsOnLeft)
			{
				return leftFieldID;
			}
			return rightFieldID;
		}
	}

	private string OppositeID => $"Elevator_{OppositeFieldID}";

	protected virtual bool IsOnLeft => CurrentFieldID == leftFieldID;

	private void OnValidate()
	{
		Vector2 vector = rightPosition;
		float y = (vector.y = base.transform.position.y);
		rightPosition = vector;
		vector = leftPosition;
		vector.y = y;
		leftPosition = vector;
	}

	protected override void Awake()
	{
		base.Awake();
		lightSpriteRenderer.DOAlpha(0f, 0f);
	}

	protected override void LoadState_Internal(int state)
	{
		if (rightFloorLever != null)
		{
			rightFloorLever.Initialize(this, rightPosition.x);
		}
		if (leftFloorLever != null)
		{
			leftFloorLever.Initialize(this, leftPosition.x);
		}
		isRidden = false;
		SetTransparentWallActive(active: false);
		lightAnimator.SetBool("Move", value: false);
	}

	protected override void LoadDefaultState()
	{
		ChangeFloorState(isOnOpposite ? State.UpOrRightFloor : State.DownOrLeftFloor);
		SetInitialPosition(FloorState);
	}

	protected override void LoadMovedState()
	{
		ChangeFloorState((!isOnOpposite) ? State.UpOrRightFloor : State.DownOrLeftFloor);
		SetInitialPosition(FloorState);
	}

	public override void StartMoveToEnd()
	{
		float endPos = ((FloorState == State.UpOrRightFloor) ? leftPosition.x : rightPosition.x);
		IsMovingWithFade = false;
		StartMove(endPos);
	}

	public override void OnStartRide()
	{
		if (!isRidden)
		{
			isRidden = true;
		}
	}

	public override void OnEndRide()
	{
	}

	public override void SetLeverPosition(float maxDistance, float endPos)
	{
	}

	protected override Vector2 GetPlayerSpawnPositionOnHit()
	{
		return (FloorState == State.UpOrRightFloor) ? rightFloorLever.transform.position : leftFloorLever.transform.position;
	}

	public Vector2 GetGroundPositionOnEnter()
	{
		float x = (IsOnLeft ? rightPosition.x : leftPosition.x);
		return new Vector2(x, floorCollider.bounds.center.y + floorCollider.bounds.size.y / 2f + Elevator.PlayerMover.ColliderBounds.size.y / 2f);
	}

	public bool IsPlayerRidden()
	{
		return Bounds.Intersects(Elevator.PlayerMover.ColliderBounds);
	}

	protected void SetTransparentWallActive(bool active)
	{
		foreach (GameObject transparentWall in transparentWalls)
		{
			transparentWall.SetActive(active);
		}
	}

	public void SetPreviousState()
	{
		Game.Instance.SaveData.WorldData.TryGetSectorData(out var data);
		data.TryGetObjectStateNum(OppositeID, out var state);
		if (base.IsMoving && !Bounds.Intersects(Elevator.PlayerMover.ColliderBounds))
		{
			ChangeState(state);
			ChangeOppositeElevatorState(state ^ 1);
		}
	}

	public async UniTask MoveOnEnterAsync()
	{
		isRidden = true;
		await UniTask.WaitUntil(() => base.IsLoaded);
		DeactivatePreviousPath();
		OnEnter();
		State state = (IsOnLeft ? State.UpOrRightFloor : State.DownOrLeftFloor);
		ChangeFloorState(state);
		SetInitialPosition(state);
		ElevatorTrigger.SetPushedPosition();
		lightSpriteRenderer.DOAlpha(1f, 0f);
		float num = (IsOnLeft ? leftPosition.x : rightPosition.x);
		float direction = Mathf.Sign(num - base.transform.position.x);
		float targetPosY = base.transform.position.x + direction * floorCollider.size.x * 1.25f;
		StartMove(num, ignoreWait: true);
		IsMovingWithFade = false;
		await UniTask.WaitUntil(() => targetPosY > base.transform.position.y != direction > 0f || !base.IsMoving);
	}

	protected virtual void OnEnter()
	{
	}

	protected override void OnStartMoving()
	{
		SetTransparentWallActive(active: true);
		lightAnimator.SetBool("Move", value: true);
		ChangeOppositeElevatorState(CurrentStateNum);
		ChangeState(CurrentStateNum ^ 1);
	}

	protected override void OnCompleteMoving()
	{
		isRidden = true;
		if (!BlockComplete)
		{
			isCompleted = true;
			IsMovingWithFade = true;
			SetTransparentWallActive(active: false);
			if (Mathf.Approximately(base.CurrentEndPos, IsOnLeft ? leftPosition.x : rightPosition.x))
			{
				ChangeOppositeElevatorState((!IsOnLeft) ? 1 : 0);
			}
			else if (!IsPlayerRidden())
			{
				ChangeOppositeElevatorState((EndState != State.DownOrLeftFloor) ? 1 : 0);
			}
		}
	}

	protected override IEnumerator BeforeMoveRoutine()
	{
		if (!IsMovingWithFade)
		{
			PlayStartSfx();
		}
		yield return lightSpriteRenderer.DOAlpha(1f, 0.5f);
		yield return new WaitForSeconds(0.5f);
	}

	protected override void FixedUpdate()
	{
		if (isRidden && !IsPlayerRidden())
		{
			isRidden = false;
			if (isCompleted)
			{
				isCompleted = false;
				OnReturn();
			}
		}
	}

	private void OnReturn()
	{
		lightAnimator.SetBool("Move", value: false);
		if (!ElevatorTrigger.IsNull())
		{
			ElevatorTrigger.Return();
		}
		lightSpriteRenderer.DOAlpha(0f, 0.5f);
	}

	private void SetInitialPosition(State state)
	{
		float x;
		switch (state)
		{
		case State.Moving:
			return;
		case State.UpOrRightFloor:
			x = rightPosition.x;
			break;
		case State.DownOrLeftFloor:
			x = leftPosition.x;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		SetInitialPosition(x);
	}

	private void ChangeOppositeElevatorState(int stateNum)
	{
		if (IsEntranceElevator && Game.Instance.SaveData.WorldData.TryGetSectorData(OppositeFieldID.SectorName, out var data))
		{
			data.SaveObjectState(OppositeID, stateNum);
			DebugLog.GeneralLog($"{OppositeID} 반대편 EV 엘베 상태 : {stateNum}");
		}
	}

	protected override void PlayMoveFXs()
	{
		PlayMoveParticle();
		PlayMoveLoopSfx();
	}

	protected override void StopMoveFXs()
	{
		StopMoveParticle();
		StopMoveSfx(!IsMovingWithFade);
	}

	public void SetSceneLoading()
	{
		IsMovingWithFade = true;
	}

	protected virtual void OnDrawGizmosSelected()
	{
		Vector2 size = floorCollider.size;
		Vector2 vector = leftPosition;
		vector.y += floorCollider.offset.y;
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(vector, size);
		vector.x = rightPosition.x;
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(vector, size);
	}

	protected override void OnDestroy()
	{
		StopMoveSfx(!IsMovingWithFade);
	}
}
