using Cysharp.Threading.Tasks;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public abstract class PlayerBullet : MonoBehaviour, IPoolableObject, IAttackSource
{
	[SerializeField]
	private bool collideWithPlatform;

	private Vector2 direction;

	private float speed;

	private float maxDistance;

	private float currentDistance;

	private bool canMove;

	protected int AttackLayer;

	public abstract string Name { get; }

	public Direction FacingDirection => DirectionX;

	protected Direction DirectionX
	{
		get
		{
			if (direction.x == 0f)
			{
				return Direction.None;
			}
			if (!(direction.x > 0f))
			{
				return Direction.Left;
			}
			return Direction.Right;
		}
	}

	protected virtual float PlatformCheckDistance { get; }

	protected static ObjectPoolManager ObjectPoolManager => Game.Instance.ObjectPoolManager;

	public string PrefabKey { get; private set; }

	public GameObject GameObject => base.gameObject;

	public void Initialize(string address, Vector2 direction, float speed, float maxDistance)
	{
		PrefabKey = address;
		this.direction = direction;
		this.speed = speed;
		this.maxDistance = maxDistance;
		currentDistance = 0f;
		AttackLayer = ~(int)LayerUtility.PlayerLayer;
	}

	public void StartMove()
	{
		canMove = true;
	}

	private void FixedUpdate()
	{
		if (!canMove)
		{
			return;
		}
		if (currentDistance >= maxDistance)
		{
			ReturnToPool();
			return;
		}
		if (collideWithPlatform)
		{
			RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position, direction, PlatformCheckDistance, LayerUtility.PlatformLayer);
			if ((object)raycastHit2D.collider != null)
			{
				Vector3 vector = direction * raycastHit2D.distance;
				base.transform.position += vector;
				Attack();
				ReturnToPool();
				return;
			}
		}
		Vector3 position = base.transform.position;
		Vector3 vector2 = direction * (speed * Time.fixedDeltaTime);
		position += vector2;
		base.transform.position = position;
		currentDistance += vector2.magnitude;
		Attack();
	}

	public void Reverse()
	{
		direction = new Vector2(0f - direction.x, direction.y);
		Vector3 localScale = base.transform.localScale;
		base.transform.localScale = new Vector3(0f - localScale.x, localScale.y, localScale.z);
		AttackLayer = LayerUtility.PlayerLayer;
	}

	protected abstract void Attack();

	protected abstract UniTask OnReturn();

	public void InitializeBeforeUse()
	{
		canMove = false;
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		canMove = false;
		OnReturn().GetAwaiter().OnCompleted(delegate
		{
			DebugLog.GeneralLog("Return to pool");
			ObjectPoolManager.Return(this);
		});
	}
}
