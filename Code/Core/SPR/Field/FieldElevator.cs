using System.Collections;
using System.Linq;
using System.Threading;
using Cinemachine;
using DG.Tweening;
using Library;
using SPR.Library;
using Spine.Unity;
using UnityEngine;

namespace SPR.Field;

public class FieldElevator : Elevator
{
	[Header("References")]
	[SerializeField]
	private SpriteRenderer floorRenderer;

	[SerializeField]
	private SpriteRenderer ceilRenderer;

	[Header("Animator")]
	[SerializeField]
	protected bool useUnityAnimator;

	[SerializeField]
	protected Animator elevatorAnimator;

	[SerializeField]
	protected SkeletonAnimation skeletonAnimation;

	[SerializeField]
	protected Animator lightAnimator;

	[Header("Lever")]
	[SerializeField]
	protected ElevatorLever upFloorLever;

	[SerializeField]
	protected ElevatorLever downFloorLever;

	[Header("Variables")]
	[SerializeField]
	protected Vector2 size;

	[SerializeField]
	private Bounds creatureBounds;

	[SerializeField]
	protected float upFloorWorldPosY;

	[SerializeField]
	protected float downFloorWorldPosY;

	[SerializeField]
	protected State defaultState;

	[SerializeField]
	private bool controlVcam;

	private ElevatorTrigger elevatorTrigger;

	private Area area;

	protected bool BlockComplete;

	private CancellationTokenSource rideTokenSource;

	private float originScreenY;

	private bool isCompleted;

	private bool isRidden;

	private bool shouldCheckEnemy;

	private bool canRide;

	private Vector2Int originPosition;

	protected bool IsActive => CurrentStateNum <= 3;

	public bool CanRide
	{
		get
		{
			if (IsActive)
			{
				return canRide;
			}
			return false;
		}
		private set
		{
			if (IsActive)
			{
				canRide = value;
			}
		}
	}

	public override string ID => $"Elevator_{originPosition}";

	public Bounds Bounds => new Bounds(base.transform.position, size);

	public override Bounds UnderBounds => new Bounds(base.transform.position + Vector3.down * (size.y / 2f - 0.125f), new Vector3(size.x, 0.125f));

	private Vector2 TriggerCenter => base.transform.position + creatureBounds.center;

	protected ElevatorTrigger ElevatorTrigger => elevatorTrigger ?? (elevatorTrigger = GetComponentInChildren<ElevatorTrigger>());

	protected Area MyArea => area ?? (area = base.transform.GetComponentInParent<Area>());

	private void OnValidate()
	{
		if (!upFloorLever.IsNull())
		{
			upFloorLever.name = base.name + " Lever - Up";
		}
		if (!downFloorLever.IsNull())
		{
			downFloorLever.name = base.name + " Lever - Down";
		}
		if (size.x != 0f && size.y != 0f)
		{
			Vector2 vector = new Vector2(size.x, 1f);
			Vector2 vector2 = new Vector2(0f, size.y / 2f - 0.5f);
			ceilingCollider.size = vector;
			floorCollider.size = vector;
			floorCollider.offset = vector2 * Vector2.down;
			if (!floorRenderer.IsNull())
			{
				floorRenderer.size = vector;
				Vector3 localPosition = floorRenderer.transform.localPosition;
				localPosition.y = 0f - vector2.y;
				floorRenderer.transform.localPosition = localPosition;
			}
			if (!ceilRenderer.IsNull())
			{
				ceilRenderer.size = vector;
				Vector3 localPosition2 = ceilRenderer.transform.localPosition;
				localPosition2.y = vector2.y;
				ceilRenderer.transform.localPosition = localPosition2;
			}
		}
	}

	private void OnDrawGizmos()
	{
		Vector3 center = new Vector3(base.transform.position.x, upFloorWorldPosY);
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(center, size);
		Vector3 center2 = new Vector3(base.transform.position.x, downFloorWorldPosY);
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(center2, size);
		if (creatureBounds.size == Vector3.zero)
		{
			creatureBounds.center = floorCollider.offset;
			creatureBounds.size = size * 2f;
		}
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(TriggerCenter, creatureBounds.size);
	}

	protected override void Awake()
	{
		base.Awake();
		originPosition = base.transform.position.ToVector2Int();
		CinemachineFramingTransposer cinemachineComponent = MyArea.Vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
		originScreenY = cinemachineComponent.m_ScreenY;
	}

	protected override void LoadState_Internal(int state)
	{
		if (upFloorLever != null)
		{
			upFloorLever.Initialize(this, upFloorWorldPosY);
		}
		if (downFloorLever != null)
		{
			downFloorLever.Initialize(this, downFloorWorldPosY);
		}
		CanRide = true;
		isRidden = false;
		if (!useUnityAnimator)
		{
			skeletonAnimation.SetAnimation("idle");
		}
		lightAnimator.SetBool("Active", value: true);
		CanRide = true;
	}

	protected override void LoadDefaultState()
	{
		Vector3 position = base.transform.position;
		position.y = ((defaultState == State.DownOrLeftFloor) ? downFloorWorldPosY : upFloorWorldPosY);
		base.transform.position = position;
		ChangeFloorState(defaultState);
	}

	protected override void LoadMovedState()
	{
		Vector3 position = base.transform.position;
		position.y = ((defaultState == State.DownOrLeftFloor) ? upFloorWorldPosY : downFloorWorldPosY);
		base.transform.position = position;
		State state = ((defaultState == State.DownOrLeftFloor) ? State.UpOrRightFloor : State.DownOrLeftFloor);
		ChangeFloorState(state);
	}

	protected override void OnStartMoving()
	{
		EditCamera();
		lightAnimator.SetBool("Move", value: true);
		if (!ElevatorTrigger.IsNull())
		{
			ElevatorTrigger.SetPushedPosition();
		}
	}

	protected override void OnCompleteMoving()
	{
		isRidden = true;
		if (!BlockComplete)
		{
			isCompleted = true;
			ReturnCamera();
			StartCoroutine(ElasticMoveRoutine());
		}
	}

	private IEnumerator ElasticMoveRoutine()
	{
		Vector2 endPos = (Vector2)base.transform.position + Vector2.down * 0.25f;
		yield return base.Rb2D.MoveWithTween(endPos, 3f / 32f, Ease.InOutSine);
		endPos = (Vector2)base.transform.position + Vector2.up * 0.5f;
		yield return base.Rb2D.MoveWithTween(endPos, 3f / 32f, Ease.InOutSine);
		endPos = (Vector2)base.transform.position + Vector2.down * 0.5f;
		yield return base.Rb2D.MoveWithTween(endPos, 3f / 32f, Ease.InOutSine);
		endPos = (Vector2)base.transform.position + Vector2.up * 0.25f;
		yield return base.Rb2D.MoveWithTween(endPos, 3f / 32f, Ease.InOutSine);
	}

	public override void OnStartRide()
	{
		if (!isRidden)
		{
			isRidden = true;
			if (FloorState == State.Moving)
			{
				EditCamera();
			}
		}
	}

	public override void OnEndRide()
	{
	}

	public override void StartMoveToEnd()
	{
		float endPos = ((FloorState == State.UpOrRightFloor) ? downFloorWorldPosY : upFloorWorldPosY);
		StartMove(endPos);
	}

	private void EditCamera()
	{
		if (controlVcam && IsPlayerRidden())
		{
			CinemachineFramingTransposer transposer = MyArea.Vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
			float num = ((originScreenY > 0.7f) ? (originScreenY - 0.7f) : 0.2f);
			float endValue = originScreenY + num + (float)(base.CurrentEndPos - base.transform.position.y).GetSign() * 0.1f;
			DOTween.To(() => transposer.m_ScreenY, delegate(float y)
			{
				transposer.m_ScreenY = y;
			}, endValue, 1f);
		}
	}

	private void ReturnCamera()
	{
		if (!controlVcam)
		{
			return;
		}
		CinemachineFramingTransposer transposer = MyArea.Vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
		if (!Mathf.Approximately(transposer.m_ScreenY, originScreenY))
		{
			DOTween.To(() => transposer.m_ScreenY, delegate(float y)
			{
				transposer.m_ScreenY = y;
			}, originScreenY, 1f);
		}
	}

	protected override Vector2 GetPlayerSpawnPositionOnHit()
	{
		return (FloorState == State.UpOrRightFloor) ? upFloorLever.transform.position : downFloorLever.transform.position;
	}

	public bool IsPlayerRidden()
	{
		return Bounds.Intersects(Elevator.PlayerMover.VisualBounds) | IsPlayerOnCeiling();
	}

	protected bool IsPlayerOnCeiling()
	{
		if (Elevator.PlayerMover.IsOnGround)
		{
			return ceilingCollider.IsTouchingLayers(LayerUtility.PlayerLayer);
		}
		return false;
	}

	protected override void FixedUpdate()
	{
		if (!IsActive)
		{
			return;
		}
		if (isRidden && !IsPlayerRidden())
		{
			isRidden = false;
			ReturnCamera();
			OnGetOff();
			if (isCompleted)
			{
				isCompleted = false;
				OnReturn(complete: true);
			}
		}
		if (!base.IsMoving)
		{
			bool num = CanRide;
			CanRide = !IsThereCreature();
			if (num != CanRide)
			{
				lightAnimator.SetBool("Active", CanRide);
			}
			if (shouldCheckEnemy && !CanRide)
			{
				shouldCheckEnemy = false;
				TryStopMove();
				OnReturn(complete: false);
			}
		}
		base.FixedUpdate();
	}

	protected virtual void OnGetOff()
	{
	}

	protected override IEnumerator BeforeMoveRoutine()
	{
		shouldCheckEnemy = true;
		yield return base.BeforeMoveRoutine();
		PlayActivateAnimation();
		yield return new WaitForSeconds(0.5f);
		shouldCheckEnemy = false;
	}

	protected void PlayActivateAnimation()
	{
		if (useUnityAnimator)
		{
			elevatorAnimator.SetTrigger("Activate");
			return;
		}
		skeletonAnimation.SetAnimation("activated", 0, loop: false);
		skeletonAnimation.AddAnimation("idle");
	}

	private void OnReturn(bool complete)
	{
		lightAnimator.SetBool("Move", value: false);
		if (!ElevatorTrigger.IsNull())
		{
			ElevatorTrigger.Return();
		}
		if (!complete)
		{
			base.CurrentEndPos = base.transform.position.y;
		}
	}

	private bool IsThereCreature()
	{
		Bounds bounds = new Bounds(TriggerCenter, creatureBounds.size);
		return Game.Instance.FieldController.Enemies.Any((Enemy e) => e.Bounds.Intersects(bounds));
	}
}
