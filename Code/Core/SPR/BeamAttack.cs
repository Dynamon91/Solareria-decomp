using System.Collections.Generic;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class BeamAttack : PoolingObject, IAttackSource
{
	[SerializeField]
	private string attackName;

	private BoxCollider2D box2D;

	private float speed;

	private Vector3 end;

	private readonly List<IDamageable> damageableList = new List<IDamageable>();

	private Vector2 lastPosition;

	public string Name => attackName;

	public Direction FacingDirection
	{
		get
		{
			if (!(base.transform.localScale.x > 0f))
			{
				return Direction.Left;
			}
			return Direction.Right;
		}
	}

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public override void InitializeBeforeUse()
	{
		base.InitializeBeforeUse();
		damageableList.Clear();
	}

	public override void ReturnToPool()
	{
		speed = 0f;
		base.ReturnToPool();
	}

	private void FixedUpdate()
	{
		Move();
	}

	public void Initialize(float speed, Vector3 end)
	{
		int sign = base.transform.localScale.x.GetSign();
		end.x += (float)sign * speed * Time.fixedDeltaTime;
		this.speed = speed;
		this.end = end;
		AttackUtility.AttackBox(base.transform.position, Box2D.size, new HitInfo(this, string.Empty), LayerUtility.PlayerLayer, damageableList);
	}

	protected void Move()
	{
		if (!(speed <= 0f))
		{
			if (base.transform.position == end)
			{
				ReturnToPool();
				return;
			}
			lastPosition = base.transform.position;
			base.transform.position = Vector3.MoveTowards(base.transform.position, end, speed * Time.fixedDeltaTime);
			AttackUtility.AttackBox(lastPosition, Box2D.size, new HitInfo(this, string.Empty), LayerUtility.PlayerLayer, damageableList);
		}
	}
}
