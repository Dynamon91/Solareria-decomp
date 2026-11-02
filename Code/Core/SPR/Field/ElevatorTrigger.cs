using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace SPR.Field;

[RequireComponent(typeof(BoxCollider2D))]
public class ElevatorTrigger : MonoBehaviour, IRideable, IGround
{
	[SerializeField]
	private float distance = 1.5f;

	private Elevator ev;

	private Coroutine moveRoutine;

	private Tween moveTween;

	private float originLocalPosY;

	private bool isRidden;

	private bool isMoving;

	private bool isMovingToReturn;

	public SoundManager.GroundMaterialType GroundMaterial => Elevator.GroundMaterial;

	public Vector2 Velocity { get; private set; }

	private Elevator Elevator => ev ?? (ev = GetComponentInParent<Elevator>());

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private void Awake()
	{
		originLocalPosY = base.transform.localPosition.y;
	}

	public void OnStartRide()
	{
		if (!isRidden && !(Elevator is FieldElevator { CanRide: false }))
		{
			isRidden = true;
			isMovingToReturn = false;
			if (moveRoutine != null)
			{
				StopCoroutine(moveRoutine);
				Velocity = Vector2.zero;
			}
			moveRoutine = StartCoroutine(PushTriggerRoutine());
		}
	}

	public void OnEndRide()
	{
		if (!isMoving)
		{
			isRidden = false;
		}
	}

	protected virtual void OnPushed()
	{
	}

	private IEnumerator PushTriggerRoutine()
	{
		OnPushed();
		isMoving = true;
		yield return new WaitForSeconds(0.2f);
		SoundManager.PlayOneShotAttached("event:/SFX/Object/Elevator/SFX_Elevator_ButtonPress", base.transform);
		Velocity = new Vector2(0f, 0f - distance * 5f);
		float endValue = originLocalPosY - distance;
		moveTween = base.transform.DOLocalMoveY(endValue, 0.2f).SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed);
		yield return moveTween.WaitForCompletion();
		Velocity = Vector2.zero;
		Elevator.StartMoveToEnd();
	}

	public void SetPushedPosition()
	{
		isRidden = true;
		isMoving = true;
		Vector3 localPosition = base.transform.localPosition;
		localPosition.y = originLocalPosY - distance;
		base.transform.localPosition = localPosition;
	}

	public void Return()
	{
		if (!isMovingToReturn)
		{
			if (moveRoutine != null)
			{
				StopCoroutine(moveRoutine);
				Velocity = Vector2.zero;
				moveTween?.Kill();
			}
			moveRoutine = StartCoroutine(ReturnRoutine());
		}
	}

	private IEnumerator ReturnRoutine()
	{
		isMovingToReturn = true;
		SoundManager.PlayOneShotAttached("event:/SFX/Object/Elevator/SFX_Elevator_ButtonReleased", base.transform);
		Velocity = new Vector2(0f, distance * 5f);
		float endValue = originLocalPosY;
		moveTween = base.transform.DOLocalMoveY(endValue, 0.2f).SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed);
		yield return moveTween.WaitForCompletion();
		Velocity = Vector2.zero;
		isMoving = false;
		isRidden = false;
		isMovingToReturn = false;
	}

	protected virtual void OnDisable()
	{
		if (moveRoutine != null)
		{
			StopCoroutine(moveRoutine);
			moveRoutine = null;
			moveTween?.Kill();
			Velocity = Vector2.zero;
		}
	}
}
