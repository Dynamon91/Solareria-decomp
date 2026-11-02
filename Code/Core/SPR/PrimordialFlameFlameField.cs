using System.Collections.Generic;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class PrimordialFlameFlameField : MonoBehaviour, IPoolableObject, IAttackSource
{
	private FrameTimer cycleTimer;

	private BoxCollider2D box2D;

	private List<IDamageable> damageableList = new List<IDamageable>();

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public string PrefabKey => "Primordial Flame_Flame Field.prefab";

	public GameObject GameObject => base.gameObject;

	public string Name => "PrimordialFlame_FlameField";

	public Direction FacingDirection => Direction.None;

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		cycleTimer = null;
		Game.Instance.ObjectPoolManager.Return(this);
	}

	private void FixedUpdate()
	{
		if (cycleTimer.IsDone)
		{
			HandleOverlap();
			cycleTimer.Reset();
			damageableList.Clear();
		}
		cycleTimer.Tick();
	}

	public void Initialize(int cycle, List<IDamageable> list)
	{
		cycleTimer = new FrameTimer(cycle);
		damageableList = list;
	}

	private void HandleOverlap()
	{
		if (cycleTimer != null)
		{
			Vector2 center = (Vector2)base.transform.position + Box2D.offset;
			Vector2 size = Box2D.size;
			AttackUtility.AttackBox(center, size, new HitInfo(this, string.Empty), LayerUtility.EnemyDefaultAttackLayer, damageableList);
		}
	}
}
