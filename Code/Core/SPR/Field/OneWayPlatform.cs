using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LapinLibrary;
using Library;
using SPR.Library;
using UnityEngine;
using UnityEngine.U2D;

namespace SPR.Field;

[RequireComponent(typeof(EdgeCollider2D), typeof(PlatformEffector2D), typeof(SpriteShapeController))]
public class OneWayPlatform : MonoBehaviour, IRideable, IGround
{
	private enum State
	{
		Active,
		Inactive,
		Contact,
		Delay
	}

	[Serializable]
	public struct Point
	{
		[Tooltip("1이면 오른쪽 경사면, -1이면 왼쪽 경사면. 0이면 평지")]
		[SerializeField]
		private int isSlope;

		[SerializeField]
		private int length;

		public bool IsSlope => isSlope != 0;

		public int Slope => isSlope;

		public int Length
		{
			get
			{
				if (isSlope == 0)
				{
					return length;
				}
				if (length % 2 == 1)
				{
					length++;
				}
				return length;
			}
		}
	}

	[SerializeField]
	private bool branchToRight;

	[SerializeField]
	private List<Point> points;

	[SerializeField]
	private bool hasBackWall;

	[SerializeField]
	private bool enabledHanging;

	[SerializeField]
	private SoundManager.GroundMaterialType groundMaterial;

	[Header("References")]
	[SerializeField]
	private SpriteShape treeProfile;

	[SerializeField]
	private SpriteShape floatingProfile;

	[SerializeField]
	private SpriteRenderer backWallObject;

	private EdgeCollider2D ec;

	private PlatformEffector2D pe;

	private SpriteShapeController ssc;

	private State currentState;

	private bool isContactWithPlayer;

	private Vector2 originPosition;

	private CancellationTokenSource tokenSource;

	private Rigidbody2D rb2D;

	private Rigidbody2D Rb2D => rb2D ?? (rb2D = GetComponent<Rigidbody2D>());

	private static PlayerController Player => Game.Instance.Player;

	private static PlayerMover PlayerMover => Game.Instance.Player.mover;

	private static int OriginLayer => LayerUtility.OneWayPlatformLayerIndex;

	public SpriteShapeController ShapeController => ssc ?? (ssc = GetComponent<SpriteShapeController>());

	public EdgeCollider2D EdgeCollider => ec ?? (ec = GetComponent<EdgeCollider2D>());

	private bool IsTryingFalling => PlayerMover.PSM.CurrentState.GetType() == typeof(PlayerJumpDownState);

	public Spline Spline => ShapeController.spline;

	public List<Point> Points => points;

	public bool BranchToRight => branchToRight;

	public bool HasBackWall => hasBackWall;

	public SpriteRenderer BackWall => backWallObject;

	public Vector2 Velocity => Rb2D.velocity;

	public SoundManager.GroundMaterialType GroundMaterial => groundMaterial;

	private void Awake()
	{
		originPosition = base.transform.position;
	}

	public void OnContact()
	{
		isContactWithPlayer = true;
		State state = currentState;
		if (state != State.Inactive && state != State.Delay && IsTryingFalling)
		{
			WaitAndResetAsync().Forget();
		}
	}

	private async UniTask WaitAndResetAsync()
	{
		await UniTask.WaitUntil(() => !PlayerMover.IsOnGround);
		await UniTask.WaitUntil(delegate
		{
			bool isRelocating = PlayerMover.IsRelocating;
			bool flag = PlayerMover.PSM.CurrentState == PlayerMover.PSM.IdleState;
			bool isOnGround = PlayerMover.IsOnGround;
			bool flag2 = PlayerMover.RelativeVelocity.y > 0.01f;
			return isRelocating || flag || isOnGround || flag2;
		});
		if (!this.IsNull())
		{
			SetState(State.Inactive);
		}
	}

	private void OnDrawGizmosSelected()
	{
		base.transform.position = base.transform.position.RoundToInt();
	}

	private void Update()
	{
		CheckPlayerStillContactWithMe();
		switch (currentState)
		{
		case State.Inactive:
			if (!PlayerMover.IsOnGround)
			{
				SetState(State.Active);
			}
			break;
		case State.Active:
			if (isContactWithPlayer)
			{
				SetState(State.Contact);
			}
			else if (PlayerMover.IsOnGround)
			{
				SetState(State.Inactive);
			}
			break;
		case State.Contact:
			if (IsTryingFalling)
			{
				SetState(State.Delay);
				WaitAndResetAsync().Forget();
			}
			else if (!isContactWithPlayer)
			{
				SetState(PlayerMover.IsOnGround ? State.Inactive : State.Active);
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case State.Delay:
			break;
		}
		EdgeCollider.enabled = !Game.Instance.DevController.IsDevMode;
	}

	private void SetState(State state, bool force = false)
	{
		if (force || currentState != state)
		{
			currentState = state;
			switch (state)
			{
			case State.Active:
			case State.Contact:
				base.gameObject.layer = OriginLayer;
				break;
			case State.Inactive:
			case State.Delay:
				base.gameObject.layer = 0;
				break;
			}
		}
	}

	private void CheckPlayerStillContactWithMe()
	{
		if (isContactWithPlayer)
		{
			ContactFilter2D contactFilter = new ContactFilter2D
			{
				useTriggers = true,
				useLayerMask = true,
				layerMask = LayerUtility.PlayerLayer,
				useDepth = false,
				useOutsideDepth = false
			};
			isContactWithPlayer = Rb2D.GetContacts(contactFilter, new Collider2D[1]) > 0;
		}
	}

	public void OnStartRide()
	{
		if (enabledHanging)
		{
			CancellationToken token = TaskUtil.RefreshToken(ref tokenSource);
			PlayHangingTweenAsync(token).Forget();
		}
	}

	public void OnEndRide()
	{
	}

	private async UniTask PlayHangingTweenAsync(CancellationToken token)
	{
		Vector2 endPos = originPosition + Vector2.down * 0.25f;
		await Rb2D.MoveWithTween(endPos, 0.25f, Ease.OutExpo, token);
		await Rb2D.MoveWithTween(originPosition, 2f, Ease.OutElastic, token);
	}
}
