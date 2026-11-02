using System.Collections.Generic;
using Library;
using SPR.Field;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class BigArrow : MonoBehaviour, IPoolableObject, IAttackSource
{
	private float speed;

	private readonly List<IDamageable> damageableList = new List<IDamageable>();

	private BoxCollider2D box2D;

	public string Name => "BigArrow";

	public Direction FacingDirection { get; private set; }

	public string PrefabKey => "BigArrow.prefab";

	public GameObject GameObject => base.gameObject;

	private BoxCollider2D Box2D => box2D ?? GetComponent<BoxCollider2D>();

	public void InitializeBeforeUse()
	{
		speed = 0f;
		FacingDirection = Direction.None;
		damageableList.Clear();
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	private void FixedUpdate()
	{
		if (speed != 0f)
		{
			Move();
			HandleOverlap();
		}
	}

	public void Shoot(float speed, Direction direction)
	{
		this.speed = speed;
		FacingDirection = direction;
		float z = Vector2.SignedAngle(Vector2.up, FacingDirection.ToVector2());
		base.transform.eulerAngles = new Vector3(0f, 0f, z);
	}

	private void Move()
	{
		Vector2 vector = speed * FacingDirection.ToVector2();
		base.transform.position += (Vector3)vector * Time.fixedDeltaTime;
	}

	private void HandleOverlap()
	{
		float num = speed * Time.fixedDeltaTime;
		float y = Box2D.size.y * num / 2f - Box2D.size.y / 2f;
		Vector2 point = (Vector2)base.transform.position + FacingDirection.ToVector2() * (Box2D.offset + new Vector2(1f, y));
		Vector2 size = new Vector2(Box2D.size.x, Box2D.size.y * num);
		float angle = Vector2.Angle(Vector2.up, FacingDirection.ToVector2());
		Collider2D[] array = Physics2D.OverlapBoxAll(point, size, angle);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].TryGetComponent<IDamageable>(out var component) && !(component is Platform))
			{
				if (!damageableList.Contains(component))
				{
					component.TryTakeDamage(new HitInfo(this, string.Empty));
					damageableList.Add(component);
				}
			}
			else if (component is Platform)
			{
				ReturnToPool();
				break;
			}
		}
	}
}
