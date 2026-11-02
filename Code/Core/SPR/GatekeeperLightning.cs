using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;
using UnityEngine.VFX;

namespace SPR;

public class GatekeeperLightning : MonoBehaviour, IAttackSource, IPoolableObject
{
	private FrameTimer timer;

	private PoolableTimeLimitObject sign;

	private readonly List<IDamageable> damageableList = new List<IDamageable>();

	private BoxCollider2D box2D;

	public string Name => "GatekeeperLightning";

	public Direction FacingDirection
	{
		get
		{
			if (!((Player.VisualBottom - (Vector2)base.transform.position).x < 0f))
			{
				return Direction.Right;
			}
			return Direction.Left;
		}
	}

	public string PrefabKey => "Gatekeeper Lightning.prefab";

	public GameObject GameObject => base.gameObject;

	public bool IsActive { get; private set; }

	private static PlayerMover Player => Game.Instance.Player.mover;

	public BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public void InitializeBeforeUse()
	{
		timer = null;
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
	}

	private void FixedUpdate()
	{
		if (timer != null)
		{
			if (timer.IsDone)
			{
				IsActive = false;
				return;
			}
			HandleOverlap();
			timer.Tick();
		}
	}

	public async UniTaskVoid Activate(int signDuration, int duration)
	{
		IsActive = false;
		Vector3 localScale = base.transform.localScale;
		float y = base.transform.position.y + Box2D.offset.y * localScale.y - Box2D.size.y * localScale.y / 2f;
		Vector2 vector = new Vector2(base.transform.position.x, y);
		sign = Game.Instance.ObjectPoolManager.GetObject<PoolableTimeLimitObject>("Attack9_Sign.prefab", vector);
		sign.Initialize("Attack9_Sign.prefab", Util.FrameToSeconds(signDuration));
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(signDuration);
		GetComponent<VisualEffect>().Play();
		timer = new FrameTimer(duration);
	}

	private void HandleOverlap()
	{
		Vector3 localScale = base.transform.localScale;
		Vector2 size = Box2D.size * localScale;
		AttackUtility.AttackBox((Vector2)base.transform.position + Box2D.offset * localScale, size, new HitInfo(this, string.Empty), LayerUtility.EnemyDefaultAttackLayer, damageableList);
	}
}
