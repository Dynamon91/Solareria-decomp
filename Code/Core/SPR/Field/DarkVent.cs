using Cysharp.Threading.Tasks;
using DG.Tweening;
using SPR.Library;
using UnityEngine;

namespace SPR.Field;

public class DarkVent : MonoBehaviour, IDamageable, IAttackSource
{
	[SerializeField]
	private float health;

	[SerializeField]
	private int cycleFrame;

	[Header("연기")]
	[SerializeField]
	private Zone smokeZone;

	[SerializeField]
	private int smokeDuration;

	[SerializeField]
	private int smokeAttackCycle;

	[SerializeField]
	private int explosionWarningFrame;

	[SerializeField]
	private float explosionRadius;

	[Header("References")]
	[SerializeField]
	private Transform spoutTransform;

	private float currentHealth;

	private FrameTimer cycle;

	private FrameTimer attackCycle;

	private BoxCollider2D box2D;

	private SpriteRenderer spriteRenderer;

	public HitType HitType => HitType.Object;

	public Vector2 VisualCenter => base.transform.position;

	public string Name => "DarkVent";

	public Direction FacingDirection => Direction.None;

	private bool IsBroken => Game.Instance.RuntimeData.IsAlreadyDieCreature(Game.Instance.CurrentFieldID, base.transform.position);

	private static ObjectPoolManager PoolManager => Game.Instance.ObjectPoolManager;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	private SpriteRenderer SpriteRenderer => spriteRenderer ?? (spriteRenderer = GetComponent<SpriteRenderer>());

	private void Awake()
	{
		if (!IsBroken)
		{
			currentHealth = health;
			cycle = new FrameTimer(cycleFrame);
			attackCycle = new FrameTimer(smokeAttackCycle);
		}
		Box2D.enabled = !IsBroken;
	}

	private void FixedUpdate()
	{
		if (cycle != null)
		{
			if (cycle.IsDone)
			{
				Spout();
				cycle.Reset();
			}
			cycle.Tick();
		}
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		currentHealth -= hitInfo.Damage;
		if (currentHealth <= 0f)
		{
			Break();
		}
		return true;
	}

	private void Spout()
	{
		Vector3 normalized = (spoutTransform.position - base.transform.position).normalized;
		float z = Vector2.SignedAngle(Vector2.down, normalized);
		PoolingParticle poolingParticle = PoolManager.GetObject<PoolingParticle>("DarkVent_Fog.prefab", spoutTransform.position);
		poolingParticle.transform.rotation = Quaternion.Euler(0f, 0f, z);
		poolingParticle.Play();
		SmokeAttack().Forget();
	}

	private void Break()
	{
		Box2D.enabled = false;
		cycle = null;
		Game.Instance.RuntimeData.AddDieCreature(Game.Instance.CurrentFieldID, base.transform.position);
		SpriteRenderer.DOColor(Color.red, Util.FrameToSeconds(explosionWarningFrame)).SetEase(Ease.Linear).OnComplete(Explode);
	}

	private async UniTaskVoid SmokeAttack()
	{
		int frame = 0;
		Zone zone = smokeZone.GetZoneOf(base.transform);
		while (frame < smokeDuration)
		{
			if (attackCycle.IsDone)
			{
				AttackUtility.AttackBox(zone.Center, zone.Size, base.transform.rotation.eulerAngles.z, new HitInfo(this), LayerUtility.PlayerLayer);
				attackCycle.Reset();
			}
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
			frame++;
			attackCycle.Tick();
		}
	}

	private void Explode()
	{
		PoolManager.GetObject<PoolingParticle>("Explosion.prefab", base.transform.position).Play();
		foreach (IDamageable item in AttackUtility.AttackCircle(base.transform.position, explosionRadius, new HitInfo(this, "Explode"), (int)LayerUtility.PlayerLayer | (int)LayerUtility.MonsterLayer))
		{
			Debug.Log(item.HitType);
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(base.transform.position, explosionRadius);
		Zone zoneOf = smokeZone.GetZoneOf(base.transform);
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(zoneOf.Center, zoneOf.Size);
	}
}
