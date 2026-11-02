using System;
using System.Collections.Generic;
using System.Linq;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class VeritaHorizontalArrow : MonoBehaviour, IPoolableObject, IAttackSource
{
	[Serializable]
	public struct VeritaArrowParameter
	{
		[field: SerializeField]
		public float ArrowSpeed { get; private set; }

		[field: SerializeField]
		public int ShootDelayFrame { get; private set; }

		[field: SerializeField]
		public float DistanceWithWall { get; private set; }
	}

	[SerializeField]
	private GameObject trailObject;

	private BoxCollider2D boxColl;

	private SkeletonAnimation sa;

	private List<IDamageable> damaged;

	private Vector2 overlapCenter;

	private Vector2 overlapSize;

	private Vector2 moveDirection;

	private VeritaArrowParameter parameter;

	private AreaBox areaBox;

	private bool moveToPortal;

	private int currentFrame;

	private float moveDistance;

	private bool isMoving;

	private bool isHit;

	private float originBoxHeight;

	private float startPortalPosX;

	private float endPortalPosX;

	public string PrefabKey => "Verita Horizontal Arrow.prefab";

	public GameObject GameObject => base.gameObject;

	public string Name => "Verita_Arrow";

	public Direction FacingDirection
	{
		get
		{
			if (!(moveDirection.x < 0f))
			{
				return Direction.Right;
			}
			return Direction.Left;
		}
	}

	private BoxCollider2D BoxCollider => boxColl ?? (boxColl = GetComponent<BoxCollider2D>());

	private SkeletonAnimation SkeletonAnimation => sa ?? (sa = GetComponentInChildren<SkeletonAnimation>());

	public void InitializeBeforeUse()
	{
	}

	public void ReturnToPool()
	{
		Vector2 size = BoxCollider.size;
		size.y = originBoxHeight;
		BoxCollider.size = size;
		Vector2 offset = BoxCollider.offset;
		offset.y = 0f;
		BoxCollider.offset = offset;
		Game.Instance.ObjectPoolManager.Return(this);
	}

	public void Initialize(VeritaArrowParameter parameter, bool moveToPortal, Vector2 veritaPosition, AreaBox areaBox, float hitHeight, List<IDamageable> damaged)
	{
		this.parameter = parameter;
		this.moveToPortal = moveToPortal;
		this.areaBox = areaBox;
		this.damaged = damaged;
		isHit = false;
		isMoving = false;
		currentFrame = 0;
		moveDistance = 0f;
		SetRotation((veritaPosition.x - base.transform.position.x).GetSign());
		if (moveToPortal)
		{
			startPortalPosX = ((moveDirection.x > 0f) ? areaBox.LowerLeft.x : areaBox.UpperRight.x);
			startPortalPosX += moveDirection.x * 1f;
			endPortalPosX = ((moveDirection.x > 0f) ? areaBox.UpperRight.x : areaBox.LowerLeft.x);
			endPortalPosX += (0f - moveDirection.x) * 1f;
			trailObject.SetActive(value: false);
		}
		Vector3 position = base.transform.position;
		position.x += moveDirection.x * parameter.DistanceWithWall;
		base.transform.position = position;
		Vector2 size = BoxCollider.size;
		originBoxHeight = size.y;
		size.y = hitHeight;
		BoxCollider.size = size;
		Vector2 offset = BoxCollider.offset;
		offset.y -= hitHeight / 2f * moveDirection.x;
		BoxCollider.offset = offset;
		base.gameObject.SetActive(value: true);
		SkeletonAnimation.SetAnimation("arrow_small_start");
	}

	private void SetRotation(int directionX)
	{
		moveDirection = new Vector2(directionX, 0f).normalized;
		float angle = moveDirection.GetAngle();
		base.transform.rotation = Quaternion.Euler(0f, 0f, angle);
	}

	private void FixedUpdate()
	{
		if (!isMoving)
		{
			if (!(moveDistance <= 0f))
			{
				if (currentFrame < 30)
				{
					currentFrame++;
				}
				else
				{
					ReturnToPool();
				}
				return;
			}
			if (currentFrame < parameter.ShootDelayFrame)
			{
				currentFrame++;
				return;
			}
			isMoving = true;
			SkeletonAnimation.SetAnimation("arrow_small", 0, loop: false);
			SkeletonAnimation.AnimationState.AddAnimation(0, "arrow_small_loop", loop: true, 0f);
		}
		Vector2 velocity = parameter.ArrowSpeed * moveDirection;
		Move(velocity);
		if (parameter.ArrowSpeed <= 60f)
		{
			overlapCenter = base.transform.position;
			overlapSize = BoxCollider.size;
		}
		else
		{
			FastSpeedSetting();
		}
		if (!areaBox.IsInArea(overlapCenter) || Mathf.Abs(moveDistance) >= 80f)
		{
			isMoving = false;
			currentFrame = 0;
		}
		else if (!isHit)
		{
			HitInfo hitInfo = new HitInfo(this);
			List<IDamageable> list = AttackUtility.AttackBox(overlapCenter + velocity.normalized * BoxCollider.offset, overlapSize, new HitInfo(this, string.Empty), (int)LayerUtility.PlayerLayer | (int)LayerUtility.PlatformLayer, damaged);
			if (list.Count > 0 && hitInfo.TypeToAttack.HasFlag(HitType.Player) && list.FirstOrDefault((IDamageable d) => d is PlayerMover) != null && Game.Instance.Player.mover.CanHit)
			{
				isHit = true;
			}
		}
	}

	private void FastSpeedSetting()
	{
		float num = parameter.ArrowSpeed * Time.fixedDeltaTime;
		Vector2 size = BoxCollider.size;
		float x = num / 2f - size.x / 2f;
		overlapCenter = base.transform.position;
		overlapCenter += moveDirection * (BoxCollider.offset + new Vector2(x, 0f));
		overlapSize = new Vector2(num + size.x, size.y);
	}

	private void Move(Vector2 velocity)
	{
		Vector3 position = base.transform.position;
		Vector2 vector = velocity * Time.fixedDeltaTime;
		moveDistance += vector.magnitude;
		position += (Vector3)vector;
		if (moveToPortal && ((moveDirection.x >= 0f && position.x >= startPortalPosX) || (moveDirection.x < 0f && position.x <= startPortalPosX)))
		{
			moveToPortal = false;
			position.x = endPortalPosX;
			SetRotation((int)(0f - moveDirection.x));
			base.transform.position = position;
			trailObject.SetActive(value: true);
		}
		else
		{
			base.transform.position = position;
		}
	}

	private void DebugDrawOverlapInfo()
	{
		Vector2 vector = overlapSize / 2f;
		Vector2 vector2 = overlapCenter - vector;
		Vector2 vector3 = vector2 + Vector2.right * vector.x;
		Debug.DrawLine(vector2, vector3, Color.magenta);
		Vector2 vector4 = vector3;
		vector3.y += overlapSize.y;
		Debug.DrawLine(vector4, vector3, Color.magenta);
		Vector2 vector5 = vector3;
		vector3.x -= overlapSize.x;
		Debug.DrawLine(vector5, vector3, Color.magenta);
		Vector2 vector6 = vector3;
		vector3.y -= overlapSize.y;
		Debug.DrawLine(vector6, vector3, Color.magenta);
	}
}
