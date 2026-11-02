using System;
using System.Collections.Generic;
using Library;
using SPR.Field;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

[RequireComponent(typeof(BoxCollider2D))]
public class Projectile : SpineObject, IPoolableObject
{
	[SerializeField]
	private Direction pivotDirection = Direction.Up;

	[SerializeField]
	private string prefabAddress;

	[SerializeField]
	private string attackKey;

	[SerializeField]
	protected bool canBeBounce;

	protected Vector2 OverlapCenter;

	protected Vector2 MoveDirection;

	protected float Speed;

	protected float Gravity;

	protected HitInfo HitInfo;

	protected bool AttackAvailable = true;

	protected bool ReturnToPoolControl;

	private BoxCollider2D box2D;

	private readonly List<IDamageable> damageableList = new List<IDamageable>();

	private LayerMask attackLayer;

	private MinMax xLimit;

	public override string Name => attackKey;

	public override Direction FacingDirection
	{
		get
		{
			if (!(MoveDirection.x < 0f))
			{
				return Direction.Right;
			}
			return Direction.Left;
		}
	}

	public Vector2 OverlapSize { get; set; }

	public float Angle { get; protected set; }

	public BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	protected static PlayerMover Player => Game.Instance.Player.mover;

	protected IReadOnlyList<IDamageable> ReadOnlyDamageableList => damageableList;

	protected Vector2 Velocity { get; private set; }

	protected static ObjectPoolManager PoolManager => Game.Instance.ObjectPoolManager;

	public bool Reversed => HitInfo.AttackerType == HitType.Player;

	public virtual string PrefabKey => prefabAddress;

	public virtual GameObject GameObject => base.gameObject;

	public event Action OnReturn;

	public virtual void InitializeBeforeUse()
	{
		HitInfo = new HitInfo(this, string.Empty);
		damageableList.Clear();
		attackLayer = (int)LayerUtility.PlayerLayer | (int)LayerUtility.MonsterLayer | (int)LayerUtility.PlatformLayer;
		xLimit = new MinMax(-9999f, 9999f);
		base.gameObject.SetActive(value: true);
	}

	public virtual void ReturnToPool()
	{
		this.OnReturn?.Invoke();
		this.OnReturn = null;
		PoolManager.Return(this);
	}

	private void FixedUpdate()
	{
		if (base.transform.position.x < xLimit.min || base.transform.position.x > xLimit.max)
		{
			ReturnToPool();
		}
		OnFixedUpdate();
		Move();
	}

	public void SetDirection(Vector2 direction)
	{
		MoveDirection = direction;
		Rotate(direction);
	}

	public void Rotate(Vector2 direction)
	{
		Angle = Vector2.SignedAngle(Vector2.up, direction);
		base.transform.eulerAngles = new Vector3(0f, 0f, Angle);
	}

	public void Launch(Vector2 direction, float speed)
	{
		MoveDirection = direction;
		Speed = speed;
		Angle = Vector2.SignedAngle(pivotDirection.ToVector2(), direction);
		base.transform.eulerAngles = new Vector3(0f, 0f, Angle);
		SetVelocity();
		OnLaunch();
	}

	public void Launch(Vector2 direction, float speed, LayerMask layerMask)
	{
		MoveDirection = direction;
		Speed = speed;
		attackLayer = layerMask;
		Angle = Vector2.SignedAngle(pivotDirection.ToVector2(), direction);
		base.transform.eulerAngles = new Vector3(0f, 0f, Angle);
		SetVelocity();
		OnLaunch();
	}

	public void Invert()
	{
		if (!canBeBounce)
		{
			if (!ReturnToPoolControl)
			{
				ReturnToPool();
			}
			return;
		}
		MoveDirection = Player.FacingDirection.ToVector2();
		Rotate(MoveDirection);
		SetVelocity();
		float current = Game.Instance.Player.PlayerStat.GetStat<Attack>().Current;
		int num = Mathf.CeilToInt((float)HitInfo.Damage * current / 2f);
		HitInfo = new HitInfo(this, num, 0f, HitInfo.GroggyDamage, AttackDirection.Facing, HitInfo.AttackAttribute);
		attackLayer = (int)attackLayer | (int)LayerUtility.ObjectLayer;
		attackLayer = (int)attackLayer | (int)LayerUtility.MonsterLayer;
		Camera mainCamera = GraphicHelper.MainCamera;
		float num2 = (float)Screen.width / (float)Screen.height;
		float num3 = mainCamera.orthographicSize * 2f * num2;
		Vector3 vector = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y);
		float reflectedProjectileDistance = GameControlParameter.Instance.ReflectedProjectileDistance;
		xLimit = new MinMax(vector.x - num3 / 2f - reflectedProjectileDistance, vector.x + num3 / 2f + reflectedProjectileDistance);
	}

	protected virtual void OnFixedUpdate()
	{
		OverlapCenter = base.transform.position;
		Direction direction = pivotDirection;
		if (direction == Direction.Left || direction == Direction.Right)
		{
			OverlapSize = new Vector2(Box2D.size.x * ((Speed <= 60f) ? 1f : (Speed * Time.fixedDeltaTime)), Box2D.size.y);
			return;
		}
		direction = pivotDirection;
		if (direction == Direction.Up || direction == Direction.Down)
		{
			OverlapSize = new Vector2(Box2D.size.x, Box2D.size.y * ((Speed <= 60f) ? 1f : (Speed * Time.fixedDeltaTime)));
		}
	}

	protected virtual void Move()
	{
		if (!(Speed <= 0f))
		{
			HandleOverlap(OverlapCenter, OverlapSize);
			base.transform.position += (Vector3)Velocity * Time.fixedDeltaTime;
			Velocity += new Vector2(0f, 0f - Gravity) * Time.fixedDeltaTime;
		}
	}

	protected void SetVelocity()
	{
		Velocity = MoveDirection * Speed;
	}

	public void HandleOverlap(Vector2 center, Vector2 size)
	{
		if (!AttackAvailable)
		{
			return;
		}
		Angle = Vector2.SignedAngle(pivotDirection.ToVector2(), MoveDirection);
		List<IDamageable> list = AttackUtility.AttackBox(center + Velocity.normalized * Box2D.offset, size, Angle, HitInfo, attackLayer, damageableList);
		if (list.Count == 0)
		{
			return;
		}
		if (HitInfo.TypeToAttack.HasFlag(HitType.Player) && list.Contains(typeof(PlayerMover)) && Player.CanHit)
		{
			if (Player.CanParry(HitInfo))
			{
				OnParry();
				Invert();
				return;
			}
			OnHit();
			if (!ReturnToPoolControl)
			{
				ReturnToPool();
			}
		}
		else if (list.Contains(typeof(Platform)) || list.Contains(typeof(TransparentWall)))
		{
			OnPlatform();
			if (!ReturnToPoolControl)
			{
				ReturnToPool();
			}
		}
		else if (list.Contains(typeof(Enemy)))
		{
			ReturnToPool();
		}
	}

	protected virtual void OnLaunch()
	{
	}

	protected virtual void OnParry()
	{
	}

	protected virtual void OnHit()
	{
	}

	protected virtual void OnPlatform()
	{
	}
}
