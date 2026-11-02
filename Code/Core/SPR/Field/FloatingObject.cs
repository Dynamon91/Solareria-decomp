using System.Collections;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR.Field;

public class FloatingObject : MonoBehaviour, IRideable, IGround
{
	protected enum State
	{
		Normal,
		Moving,
		Floating
	}

	[SerializeField]
	private float marginFromSurface;

	[Header("Floating")]
	[SerializeField]
	private float floatingAmplitude;

	[SerializeField]
	private float floatingDuration;

	[Header("Bouncing")]
	[SerializeField]
	private float bouncingAmplitude;

	[SerializeField]
	private float bouncingDuration;

	[SerializeField]
	private ParticleSystem jumpParticle;

	[SerializeField]
	private EventReference jumpEvent;

	private Water water;

	private Vector2 originPos;

	private Rigidbody2D rb2D;

	private Coroutine shakeRoutine;

	private Vector2 PivotPosition
	{
		get
		{
			float y = water.GetSurfaceYPosition() + marginFromSurface;
			return new Vector2(originPos.x, y);
		}
	}

	protected State CurrentState { get; private set; }

	public virtual SoundManager.GroundMaterialType GroundMaterial => SoundManager.GroundMaterialType.Wood;

	public Vector2 Velocity => rb2D.velocity;

	private void Awake()
	{
		Initialize();
	}

	protected virtual void Start()
	{
		StartCoroutine(FloatingRoutine());
	}

	private void Initialize()
	{
		rb2D = GetComponent<Rigidbody2D>();
		originPos = base.transform.position;
		water = FindWater();
		CurrentState = State.Normal;
	}

	public virtual void OnStartRide()
	{
		if (shakeRoutine != null)
		{
			StopCoroutine(shakeRoutine);
			shakeRoutine = null;
		}
		shakeRoutine = StartCoroutine(BouncingRoutine());
		jumpParticle.Play();
		if (!jumpEvent.IsNull)
		{
			Game.Instance.SoundManager.PlayOneShotAttached(jumpEvent, base.transform);
		}
	}

	public void OnEndRide()
	{
	}

	private IEnumerator FloatingRoutine()
	{
		while (CurrentState != State.Moving)
		{
			yield return rb2D.PivotMoveWithTween(() => PivotPosition, Vector2.down * floatingAmplitude, floatingDuration, Ease.InOutSine);
			yield return rb2D.PivotMoveWithTween(() => PivotPosition, Vector2.up * floatingAmplitude, floatingDuration, Ease.InOutSine);
		}
		rb2D.velocity = Vector2.zero;
	}

	private IEnumerator BouncingRoutine()
	{
		CurrentState = State.Moving;
		yield return PivotMove(0f - bouncingAmplitude, bouncingDuration / 4f, Ease.OutCubic);
		yield return PivotMove(bouncingAmplitude, bouncingDuration / 2f, Ease.OutSine);
		CurrentState = State.Floating;
		shakeRoutine = StartCoroutine(FloatingRoutine());
	}

	private Water FindWater()
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(originPos + Vector2.up, Vector2.down, 100f, LayerUtility.ObjectLayer);
		if ((bool)raycastHit2D && raycastHit2D.transform.TryGetComponent<Water>(out var component))
		{
			return component;
		}
		DebugLog.GeneralLogError(base.name + "의 밑에 물이 존재하지 않습니다!");
		return null;
	}

	protected IEnumerator PivotMove(float localPosY, float duration, Ease ease)
	{
		yield return rb2D.PivotMoveWithTween(() => PivotPosition, Vector2.up * localPosY, duration, ease);
	}

	private void OnDrawGizmosSelected()
	{
		Initialize();
		Vector2 pivotPosition = PivotPosition;
		pivotPosition.x = pivotPosition.x.SnapToHalf();
		base.transform.position = pivotPosition;
	}
}
