using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class LightningAttack : MonoBehaviour, IAttackSource, IPoolableObject
{
	[SerializeField]
	private string signAddress = "Attack9_Sign.prefab";

	[SerializeField]
	private string prefabAddress;

	[SerializeField]
	private string attackKey;

	[SerializeField]
	private ParticleSystem[] particles;

	protected FrameTimer Timer;

	private BoxCollider2D box2D;

	private readonly List<IDamageable> damageableList = new List<IDamageable>();

	private PoolableTimeLimitObject sign;

	private CancellationTokenSource cts;

	public string Name => attackKey;

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

	public bool IsActive { get; private set; }

	public BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	private static PlayerMover Player => Game.Instance.Player.mover;

	public string PrefabKey => prefabAddress;

	public GameObject GameObject => base.gameObject;

	public virtual void InitializeBeforeUse()
	{
		Timer = null;
		IsActive = true;
		damageableList.Clear();
		cts = new CancellationTokenSource();
		base.gameObject.SetActive(value: true);
	}

	public virtual void ReturnToPool()
	{
		IsActive = false;
		Timer = null;
		cts.Cancel();
		Game.Instance.ObjectPoolManager.Return(this);
	}

	private void FixedUpdate()
	{
		if (Timer != null)
		{
			if (Timer.IsDone && particles.All((ParticleSystem p) => !p.isPlaying))
			{
				ReturnToPool();
			}
			else if (!Timer.IsDone)
			{
				HandleOverlap();
				Timer.Tick();
			}
		}
	}

	public async UniTaskVoid Activate(int signDuration, int duration)
	{
		IsActive = false;
		if (!signAddress.IsNullOrEmpty())
		{
			Vector3 localScale = base.transform.localScale;
			float y = base.transform.position.y + Box2D.offset.y * localScale.y - Box2D.size.y * localScale.y / 2f;
			Vector2 vector = new Vector2(base.transform.position.x, y);
			sign = Game.Instance.ObjectPoolManager.GetObject<PoolableTimeLimitObject>(signAddress, vector);
			sign.Initialize(signAddress, Util.FrameToSeconds(signDuration));
			await UniTaskExtension.WaitForFixedUpdatesOrCancel(signDuration, cts.Token);
		}
		ParticleSystem[] array = particles;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Play();
		}
		Timer = new FrameTimer(duration);
	}

	public void Cancel()
	{
		cts?.Cancel();
	}

	private void HandleOverlap()
	{
		if (!cts.IsCancellationRequested)
		{
			Vector3 localScale = base.transform.localScale;
			Vector2 size = Box2D.size * localScale;
			Vector2 center = (Vector2)base.transform.position + Box2D.offset * localScale;
			if ((!(Player.PSM.CurrentState is PlayerStaggerHitState) || !attackKey.Contains("Attack9")) && !Player.IsSpecialAttackTaskValid())
			{
				AttackUtility.AttackBox(center, size, new HitInfo(this), LayerUtility.EnemyDefaultAttackLayer, damageableList);
			}
		}
	}
}
