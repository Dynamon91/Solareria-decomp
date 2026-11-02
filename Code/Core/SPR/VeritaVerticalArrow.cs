using System;
using System.Collections.Generic;
using System.Linq;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class VeritaVerticalArrow : MonoBehaviour, IPoolableObject, IAttackSource
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

	private BoxCollider2D boxColl;

	private SkeletonAnimation sa;

	private List<IDamageable> damaged;

	private Vector2 overlapCenter;

	private Vector2 overlapSize;

	private Vector2 moveDirection;

	private VeritaArrowParameter parameter;

	private AreaBox areaBox;

	private int currentFrame;

	private float moveDistance;

	private bool isMoving;

	private bool isHit;

	private float originBoxHeight;

	private float startPortalPosX;

	private float endPortalPosX;

	public string PrefabKey => "Verita Vertical Arrow.prefab";

	public GameObject GameObject => base.gameObject;

	public string Name => "Verita_Arrow_Down_Kick";

	public Direction FacingDirection => Direction.None;

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

	public void Initialize(VeritaArrowParameter parameter, AreaBox areaBox, float hitHeight, List<IDamageable> damaged)
	{
		this.parameter = parameter;
		this.areaBox = areaBox;
		this.damaged = damaged;
		isHit = false;
		isMoving = false;
		currentFrame = 0;
		moveDistance = 0f;
		SetRotation(0, -1);
		Vector3 position = base.transform.position;
		position.y += moveDirection.y * parameter.DistanceWithWall;
		base.transform.position = position;
		Vector2 size = BoxCollider.size;
		originBoxHeight = size.y;
		size.y = hitHeight;
		BoxCollider.size = size;
		base.gameObject.SetActive(value: true);
		SkeletonAnimation.SetAnimation("arrow_small_start");
	}

	private void SetRotation(int directionX, int directionY)
	{
		moveDirection = new Vector2(directionX, directionY).normalized;
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
		if (!areaBox.IsInArea(overlapCenter - moveDirection * overlapSize / 2f) || Mathf.Abs(moveDistance) >= 80f)
		{
			isMoving = false;
			currentFrame = 0;
		}
		else if (!isHit)
		{
			HitInfo hitInfo = new HitInfo(this);
			List<IDamageable> list = AttackUtility.AttackBox(overlapCenter + velocity.normalized * BoxCollider.offset, overlapSize, new HitInfo(this), (int)LayerUtility.PlayerLayer | (int)LayerUtility.PlatformLayer, damaged);
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
		float y = num / 2f - size.y / 2f;
		overlapCenter = base.transform.position;
		overlapCenter += moveDirection * (BoxCollider.offset + new Vector2(0f, y));
		overlapSize = new Vector2(size.x, size.y) + Vector2.up * num;
	}

	private void Move(Vector2 velocity)
	{
		Vector3 position = base.transform.position;
		Vector2 vector = velocity * Time.fixedDeltaTime;
		moveDistance += vector.magnitude;
		position += (Vector3)vector;
		base.transform.position = position;
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
