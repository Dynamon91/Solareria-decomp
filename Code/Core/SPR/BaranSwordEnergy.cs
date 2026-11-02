using Library;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class BaranSwordEnergy : MonoBehaviour, IAttackSource, IPoolableObject
{
	private FrameTimer timer;

	private int interval;

	private BoxCollider2D box2D;

	public string Name => "Baran_SwordEnergy";

	public Direction FacingDirection => Direction.Left;

	public string PrefabKey => "BaranSwordEnergy.prefab";

	public GameObject GameObject => base.gameObject;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	private void FixedUpdate()
	{
		if (timer.IsDone)
		{
			ReturnToPool();
		}
		if (timer.PassedFrame % interval == 0)
		{
			HandleOverlap();
		}
		timer.Tick();
	}

	public void Initialize(int duration, int interval)
	{
		timer = new FrameTimer(duration);
		this.interval = interval;
	}

	private void HandleOverlap()
	{
		Collider2D collider2D = Physics2D.OverlapBox((Vector2)base.transform.position + Box2D.offset, Box2D.size, 0f, LayerUtility.PlayerLayer);
		if (!collider2D.IsNull() && collider2D.TryGetComponent<PlayerMover>(out var component))
		{
			component.TryTakeDamage(new HitInfo(this, string.Empty));
		}
	}
}
