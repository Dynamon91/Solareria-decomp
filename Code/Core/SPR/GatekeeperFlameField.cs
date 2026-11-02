using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class GatekeeperFlameField : MonoBehaviour, IPoolableObject, IAttackSource
{
	[SerializeField]
	private string address;

	[SerializeField]
	private string attackKey;

	[SerializeField]
	private ParticleSystem flame;

	[SerializeField]
	private ParticleSystem ember;

	[SerializeField]
	private MinMax range;

	[SerializeField]
	private MinMax flameRate;

	[SerializeField]
	private MinMax flameCount;

	[SerializeField]
	private MinMax emberRate;

	[SerializeField]
	private MinMax emberCount;

	private FrameTimer lifeTimer;

	private FrameTimer cycleTimer;

	private BoxCollider2D box2D;

	public BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	public string PrefabKey => address;

	public GameObject GameObject => base.gameObject;

	public string Name => attackKey;

	public Direction FacingDirection => Direction.None;

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		lifeTimer = null;
		cycleTimer = null;
		WaitForEffectEnd().Forget();
	}

	private void FixedUpdate()
	{
		if (lifeTimer == null)
		{
			return;
		}
		if (lifeTimer.IsDone)
		{
			ReturnToPool();
			return;
		}
		if (cycleTimer.IsDone)
		{
			HandleOverlap();
			cycleTimer.Reset();
		}
		lifeTimer.Tick();
		cycleTimer.Tick();
	}

	public void Initialize(int duration, int damageCycle, float width, bool setParticle = true)
	{
		Box2D.size = new Vector2(width, Box2D.size.y);
		lifeTimer = new FrameTimer(duration);
		cycleTimer = new FrameTimer(damageCycle);
		if (setParticle)
		{
			width /= 2f;
			float num = Mathf.Clamp(width, range.min, range.max) / range.max;
			ParticleSystem.ShapeModule shape = flame.shape;
			shape.radius = Mathf.Clamp(width, range.min, range.max);
			ParticleSystem.EmissionModule emission = flame.emission;
			ParticleSystem.MinMaxCurve rateOverTime = emission.rateOverTime;
			rateOverTime.constant = flameRate.min + (flameRate.max - flameRate.min) * num;
			emission.rateOverTime = rateOverTime;
			ParticleSystem.Burst burst = emission.GetBurst(0);
			ParticleSystem.MinMaxCurve count = burst.count;
			count.constant = flameCount.min + (flameCount.max - flameCount.min) * num;
			burst.count = count;
			emission.SetBurst(0, burst);
			ParticleSystem.ShapeModule shape2 = ember.shape;
			shape2.radius = Mathf.Clamp(width, range.min, range.max);
			ParticleSystem.EmissionModule emission2 = ember.emission;
			ParticleSystem.MinMaxCurve rateOverTime2 = emission2.rateOverTime;
			rateOverTime2.constant = emberRate.min + (emberRate.max - emberRate.min) * num;
			emission2.rateOverTime = rateOverTime2;
			ParticleSystem.Burst burst2 = emission2.GetBurst(0);
			ParticleSystem.MinMaxCurve count2 = burst2.count;
			count2.constant = emberCount.min + (emberCount.max - emberCount.min) * num;
			burst2.count = count2;
			emission2.SetBurst(0, burst2);
		}
		HandleOverlap();
	}

	private void HandleOverlap()
	{
		AttackUtility.AttackBox((Vector2)base.transform.position + Box2D.offset, Box2D.size, new HitInfo(this, string.Empty), LayerUtility.PlayerLayer);
	}

	private async UniTaskVoid WaitForEffectEnd()
	{
		if (flame.IsNull())
		{
			Game.Instance.ObjectPoolManager.Return(this);
			return;
		}
		await UniTask.WaitWhile(() => flame.isPlaying);
		Game.Instance.ObjectPoolManager.Return(this);
	}
}
