using Library;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class NormalProjectile : MonoBehaviour, IPoolableObject, IAttackSource
{
	private Vector3 direction;

	private float speed;

	private CapsuleCollider2D capsule2D;

	public string Name => "MoomooFlower_Projectile";

	public string PrefabKey => "Assets/Prefabs/Boss/StoneProjectile.prefab";

	public GameObject GameObject => base.gameObject;

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

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	private void Awake()
	{
		if (TryGetComponent<CapsuleCollider2D>(out var component))
		{
			capsule2D = component;
		}
	}

	private void FixedUpdate()
	{
		base.transform.position += direction * (speed * Time.fixedDeltaTime);
		HandleOverlap();
	}

	public void Shoot(Vector3 dir, float shootSpeed)
	{
		direction = dir.normalized;
		float num = Vector2.Angle(Vector2.up, direction);
		base.transform.eulerAngles = new Vector3(0f, 0f, num * (0f - Mathf.Sign(direction.x)));
		speed = shootSpeed;
	}

	private void HandleOverlap()
	{
		Transform transform = base.transform;
		Collider2D collider2D = Physics2D.OverlapCapsule((Vector2)transform.position + capsule2D.offset, capsule2D.size, capsule2D.direction, transform.eulerAngles.z, (int)LayerUtility.PlatformLayer | (int)LayerUtility.PlayerLayer);
		if ((object)collider2D == null)
		{
			return;
		}
		if (collider2D.TryGetComponent<PlayerMover>(out var component))
		{
			bool canHit = component.CanHit;
			(component.transform.position - transform.position).ToDirection();
			if (canHit)
			{
				ReturnToPool();
			}
		}
		else
		{
			ReturnToPool();
		}
	}
}
