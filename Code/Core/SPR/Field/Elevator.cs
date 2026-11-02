using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using LapinLibrary;
using Library;
using SPR.Library;
using SPR.Save;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SPR.Field;

public abstract class Elevator : MonoBehaviour, IRideable, IGround, IStateSavable, IAttackSource
{
	public enum State
	{
		DownOrLeftFloor,
		Moving,
		UpOrRightFloor
	}

	[SerializeField]
	protected float speed;

	[SerializeField]
	private SoundManager.GroundMaterialType groundMaterial;

	[Header("References")]
	[SerializeField]
	protected BoxCollider2D floorCollider;

	[SerializeField]
	protected BoxCollider2D ceilingCollider;

	[SerializeField]
	protected ParticleSystem moveParticle;

	[SerializeField]
	private EventReference startEvent;

	[SerializeField]
	private EventReference loopEvent;

	[SerializeField]
	private EventReference endEvent;

	private Rigidbody2D rb2D;

	protected State FloorState;

	protected int CurrentStateNum;

	private float currentEndPos;

	private CancellationTokenSource rigidbodyTokenSource;

	private Coroutine moveRoutine;

	private List<Collider2D> colliders;

	private List<Vector2> previousPathRange;

	private EventInstance loopInstance;

	private EventInstance pastLoopInstance;

	public virtual string Name => "Elevator";

	public Direction FacingDirection => Direction.None;

	public virtual string ID => $"Elevator_{Game.FieldID}";

	public Vector2 Velocity => Rb2D.velocity;

	public SoundManager.GroundMaterialType GroundMaterial => groundMaterial;

	protected static PlayerMover PlayerMover => Game.Instance.Player.mover;

	public abstract Bounds UnderBounds { get; }

	public bool IsMoved { get; private set; }

	public bool IsMoving => FloorState == State.Moving;

	public virtual bool IsVertical => true;

	protected Rigidbody2D Rb2D => rb2D ?? (rb2D = GetComponent<Rigidbody2D>());

	protected float CurrentEndPos
	{
		get
		{
			return currentEndPos;
		}
		set
		{
			currentEndPos = value;
		}
	}

	protected bool IsLoaded { get; private set; }

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	protected virtual void Awake()
	{
	}

	protected virtual void OnDestroy()
	{
		StopMoveSfx(includeStop: false);
	}

	public void LoadState(int state)
	{
		switch (state)
		{
		case 0:
			LoadDefaultState();
			break;
		case 1:
			LoadMovedState();
			break;
		}
		CurrentStateNum = state;
		LoadState_Internal(state);
		CurrentEndPos = (IsVertical ? base.transform.position.y : base.transform.position.x);
		previousPathRange = new List<Vector2>();
		UniTask.Yield(PlayerLoopTiming.LastFixedUpdate).GetAwaiter().OnCompleted(delegate
		{
			ModifyElevatorToPath();
			IsLoaded = true;
		});
	}

	protected virtual void LoadState_Internal(int state)
	{
	}

	public void ChangeState(int state)
	{
		CurrentStateNum = state;
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
			DebugLog.GeneralLog($"{ID} EV 현재 상태 : {state}");
		}
	}

	protected void ChangeFloorState(State state)
	{
		DebugLog.GeneralLog($"ID:{ID} EV Floor 이전:{FloorState} 현재:{state}");
		FloorState = state;
	}

	protected abstract void LoadDefaultState();

	protected abstract void LoadMovedState();

	public abstract void OnStartRide();

	public abstract void OnEndRide();

	protected abstract Vector2 GetPlayerSpawnPositionOnHit();

	public virtual void SetLeverPosition(float maxDistance, float endPos)
	{
		float num = (IsVertical ? base.transform.position.y : base.transform.position.x);
		if (!(Mathf.Abs(endPos - num) <= maxDistance))
		{
			float num2 = Mathf.Sign(num - endPos);
			Vector3 position = base.transform.position;
			position.y = endPos + num2 * maxDistance;
			base.transform.position = position;
		}
	}

	public void StartMove(float endPos, bool ignoreWait = false, bool ignoreState = false)
	{
		State nextState = GetNextState(endPos);
		if (ignoreState || FloorState != nextState)
		{
			CurrentEndPos = endPos;
			Vector2 endPos2 = (IsVertical ? new Vector2(base.transform.position.x, endPos) : new Vector2(endPos, base.transform.position.y));
			TryStopMove();
			moveRoutine = StartCoroutine(MoveRoutine(endPos2, nextState, ignoreWait));
		}
	}

	protected bool TryStopMove()
	{
		if (moveRoutine != null)
		{
			rigidbodyTokenSource?.Cancel();
			StopCoroutine(moveRoutine);
			StopMoveSfx(includeStop: false);
			moveRoutine = null;
			return true;
		}
		return false;
	}

	public abstract void StartMoveToEnd();

	public bool IsOnSameFloor(float endPos)
	{
		return FloorState == GetNextState(endPos);
	}

	protected virtual void OnStartMoving()
	{
	}

	protected virtual void OnCompleteMoving()
	{
	}

	public void SetColliderActive(bool value)
	{
		if (colliders.IsNullOrEmpty())
		{
			colliders = GetComponentsInChildren<Collider2D>().ToList();
		}
		colliders.ForEach(delegate(Collider2D c)
		{
			c.enabled = value;
		});
	}

	protected void SetInitialPosition(float targetPos)
	{
		Vector3 position = base.transform.position;
		if (IsVertical)
		{
			position.y = targetPos;
		}
		else
		{
			position.x = targetPos;
		}
		base.transform.position = position;
		CurrentEndPos = targetPos;
	}

	protected State GetNextState(float endPosValue)
	{
		if (Mathf.Abs(endPosValue - CurrentEndPos) < 0.01f)
		{
			if (FloorState == State.Moving)
			{
				float num = (IsVertical ? base.transform.position.y : base.transform.position.x);
				return (endPosValue > num) ? State.UpOrRightFloor : State.DownOrLeftFloor;
			}
			return FloorState;
		}
		return (endPosValue > CurrentEndPos) ? State.UpOrRightFloor : State.DownOrLeftFloor;
	}

	private IEnumerator MoveRoutine(Vector2 endPos, State nextState, bool ignoreWait)
	{
		OnStartMoving();
		if (!ignoreWait)
		{
			yield return BeforeMoveRoutine();
		}
		ChangeFloorState(State.Moving);
		yield return MoveRoutine_Internal(Rb2D, endPos, speed, TaskUtil.RefreshToken(ref rigidbodyTokenSource));
		ChangeFloorState(nextState);
		IsMoved = true;
		OnCompleteMoving();
	}

	protected virtual IEnumerator BeforeMoveRoutine()
	{
		yield return new WaitForSeconds(0.25f);
	}

	protected virtual IEnumerator MoveRoutine_Internal(Rigidbody2D rb, Vector2 endPos, float speed, CancellationToken token)
	{
		DeactivatePreviousPath();
		PlayMoveFXs();
		yield return rb.Move(endPos, 0f, speed, token);
		StopMoveFXs();
		ModifyElevatorToPath();
	}

	protected virtual void FixedUpdate()
	{
		if (!IsVertical || !Game.Instance.InputManager.IsCurrentControlTarget(Game.Instance.Player) || FloorState != State.Moving || Rb2D.velocity.y >= 0f)
		{
			return;
		}
		if (PlayerMover.IsOnGround && UnderBounds.Intersects(PlayerMover.ColliderBounds))
		{
			SetColliderActive(value: false);
			PlayerMover.TryTakeDamage(new HitInfo(this));
			Game.Instance.Player.SetPositionAsync(GetPlayerSpawnPositionOnHit(), delegate
			{
				SetColliderActive(value: true);
			}).Forget();
		}
		foreach (Enemy enemy in Game.Instance.FieldController.Enemies)
		{
			if (UnderBounds.Intersects(enemy.Bounds))
			{
				enemy.TryTakeDamage(new HitInfo(this, enemy.MaxHealth * 2, 0f, 0f, AttackDirection.Both, AttackAttribute.Default));
			}
		}
	}

	protected void ModifyElevatorToPath()
	{
		Bounds bounds = floorCollider.bounds;
		float x = bounds.min.x;
		float x2 = bounds.max.x;
		previousPathRange.Clear();
		for (float num = x - 1f; num < x2; num += 1f)
		{
			previousPathRange.Add(new Vector2(num, bounds.max.y));
		}
		Game.Instance.FieldController.ModifyPathGraphs(previousPathRange, setGround: true);
	}

	protected void DeactivatePreviousPath()
	{
		if (previousPathRange.Count <= 0)
		{
			return;
		}
		Tilemap component = Game.Instance.FieldController.Platform.GetComponent<Tilemap>();
		List<Vector2> list = new List<Vector2>();
		foreach (Vector2 item in previousPathRange)
		{
			Vector3Int position = new Vector3Int((int)item.x, (int)item.y - 1);
			if (!component.HasTile(position))
			{
				list.Add(item);
			}
		}
		Game.Instance.FieldController.ModifyPathGraphs(list, setGround: false);
		previousPathRange.Clear();
	}

	protected virtual void PlayMoveFXs()
	{
		PlayMoveParticle();
		PlayStartSfx();
		PlayMoveLoopSfx();
	}

	protected virtual void StopMoveFXs()
	{
		StopMoveParticle();
		StopMoveSfx();
	}

	protected void PlayMoveParticle()
	{
		if (moveParticle.IsNull())
		{
			DebugLog.GeneralLogWarning(base.name + "(" + ID + ")가 움직일 때 재생할 파티클이 없어서 재생하지 않습니다!");
			return;
		}
		moveParticle.gameObject.SetActive(value: true);
		moveParticle.Play();
	}

	protected void StopMoveParticle()
	{
		if (moveParticle.IsNull())
		{
			DebugLog.GeneralLogWarning(base.name + "(" + ID + ")가 움직일 때 재생할 파티클이 없어서 멈추지 않습니다!");
		}
		else
		{
			moveParticle.Stop();
		}
	}

	protected void PlayMoveLoopSfx()
	{
		if (loopEvent.IsNull)
		{
			DebugLog.GeneralLogWarning(base.name + "(" + ID + ")가 움직일 때 루프할 사운드가 없어서 재생하지 않습니다!");
			return;
		}
		if (loopInstance.isValid())
		{
			pastLoopInstance = loopInstance;
		}
		loopInstance = SoundManager.PlayOneShotAttached(loopEvent, base.transform, forcedOnSceneLoad: true);
	}

	protected void PlayStartSfx()
	{
		if (startEvent.IsNull)
		{
			DebugLog.GeneralLogWarning(base.name + "(" + ID + ")가 움직이기 시작할 때 재생할 사운드가 없어서 재생하지 않습니다!");
		}
		else
		{
			SoundManager.PlayOneShotAttached(startEvent, base.transform);
		}
	}

	protected void StopMoveSfx(bool includeStop = true)
	{
		if (pastLoopInstance.isValid())
		{
			pastLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			pastLoopInstance.release();
		}
		if (loopInstance.isValid())
		{
			loopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			loopInstance.release();
		}
		if (includeStop)
		{
			if (endEvent.IsNull)
			{
				DebugLog.GeneralLogWarning(base.name + "(" + ID + ")가 멈출 때 재생할 사운드가 없어서 재생하지 않습니다!");
			}
			else
			{
				SoundManager.PlayOneShotAttached(endEvent, base.transform);
			}
		}
	}
}
