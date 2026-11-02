using Cinemachine;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using SPR.Field;
using SPR.Library;
using SPR.ObjectPool;
using SPR.UI;
using UnityEngine;

namespace SPR;

public class CharonRock : SpineObject, IPoolableObject, IDamageable
{
	[Header("SFX")]
	[SerializeField]
	private EventReference fallingEvent;

	[SerializeField]
	private EventReference impactEvent;

	[SerializeField]
	private EventReference breakEvent;

	[SerializeField]
	private EventReference hitEvent;

	private Charon charon;

	private string rockType;

	private int windType;

	private CharonRockParameter rockParameter;

	private int hp;

	private int maxHp;

	private FrameTimer destroyTimer;

	private BoxCollider2D box2D;

	private CinemachineImpulseSource impulseSource;

	private CreatureCanvas creatureCanvas;

	public override string Name => "CharonRock";

	public HitType HitType
	{
		get
		{
			if (destroyTimer != null)
			{
				return HitType.Platform;
			}
			return HitType.Object;
		}
	}

	public bool IsFalling { get; private set; }

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	private CinemachineImpulseSource ImpulseSource => impulseSource ?? (impulseSource = GetComponent<CinemachineImpulseSource>());

	private static ObjectPoolManager PoolManager => Game.Instance.ObjectPoolManager;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public string PrefabKey => "Charon Rock.prefab";

	public GameObject GameObject => base.gameObject;

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
		Box2D.isTrigger = true;
		rockType = (CryptoRandom.RandomBool() ? "rock1" : "rock2");
		windType = CryptoRandom.Range(0, 3) + 1;
	}

	public void ReturnToPool()
	{
		destroyTimer = null;
		Game.Instance.ObjectPoolManager.Return(this);
	}

	private void FixedUpdate()
	{
		if (destroyTimer != null)
		{
			if (destroyTimer.IsDone)
			{
				ReturnToPool();
				return;
			}
			destroyTimer.Tick();
		}
		if (IsFalling)
		{
			Vector3 vector = Vector3.down * rockParameter.speed;
			base.transform.position += vector * Time.fixedDeltaTime;
			HandleOverlap();
		}
	}

	public async UniTaskVoid StartFalling(Charon charon, CharonRockParameter parameter, int sortingOrder)
	{
		if ((object)this.charon == null)
		{
			this.charon = charon;
		}
		rockParameter = parameter;
		int clearCount = Game.Instance.SaveData.ClearCount;
		maxHp = Mathf.RoundToInt((float)DataManager.Instance.GetEnemyStatDataByClearCount(clearCount, "CharonRock").HP * DataManager.Instance.GetEnemyMultiplier(clearCount).Health);
		hp = maxHp;
		if ((object)creatureCanvas == null)
		{
			creatureCanvas = await AddressableHelper.InstantiateAsync<CreatureCanvas>("Creature Canvas.prefab", base.transform);
		}
		creatureCanvas.Initialize(Box2D.size.y, 0);
		base.SortingOrder = sortingOrder;
		PoolableTimeLimitObject poolableTimeLimitObject = PoolManager.GetObject<PoolableTimeLimitObject>("VFX/Charon_CobbleFalling.prefab", base.transform.position);
		poolableTimeLimitObject.Initialize("VFX/Charon_CobbleFalling.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(rockParameter.rockWarningFrame);
		SetAnimation(rockType + "_start", loop: false);
		AddAnimation(rockType + "_start_loop");
		EnableVFX($"falling_wind_{windType}");
		IsFalling = true;
		SoundManager.PlayOneShotAttached(fallingEvent, base.transform);
	}

	private void HandleOverlap()
	{
		Collider2D[] array = Physics2D.OverlapBoxAll(VisualCenter, Box2D.size, 0f);
		if (array == null || array.Length == 0)
		{
			return;
		}
		Collider2D[] array2 = array;
		foreach (Collider2D collider2D in array2)
		{
			if (collider2D.TryGetComponent<CharonRock>(out var _))
			{
				continue;
			}
			if (collider2D.TryGetComponent<PlayerMover>(out var component2) && !charon.IsDead)
			{
				if (!component2.IsImmortal)
				{
					component2.TryTakeDamage(new HitInfo(this, string.Empty));
				}
				DestroyRock();
				break;
			}
			if (collider2D.TryGetComponent<SPR.Field.Platform>(out var _))
			{
				Box2D.isTrigger = false;
				IsFalling = false;
				Impact();
				SoundManager.PlayOneShotAttached(impactEvent, base.transform);
			}
		}
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		hp -= hitInfo.Damage;
		creatureCanvas.UpdateHealthBar(hp, maxHp, hitInfo.Damage);
		if (hp <= 0)
		{
			DestroyRock();
			return true;
		}
		SoundManager.PlayOneShotAttached(hitEvent, base.transform);
		return true;
	}

	private void Impact()
	{
		SetAnimation(rockType + "_end", loop: false);
		AddAnimation(rockType + "_end_loop");
		PoolableTimeLimitObject poolableTimeLimitObject = PoolManager.GetObject<PoolableTimeLimitObject>("VFX/Charon_RockDust.prefab", VisualCenter - new Vector2(0f, Box2D.size.y / 2f));
		poolableTimeLimitObject.Initialize("VFX/Charon_RockDust.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
		ImpulseSource.GenerateImpulse();
		DisableVFX($"falling_wind_{windType}");
		if (charon.IsDead)
		{
			DestroyRock();
		}
	}

	private void DestroyRock()
	{
		IsFalling = false;
		Box2D.isTrigger = true;
		PoolableTimeLimitObject poolableTimeLimitObject = PoolManager.GetObject<PoolableTimeLimitObject>("VFX/Charon_RockBreak.prefab", base.transform.position);
		poolableTimeLimitObject.Initialize("VFX/Charon_RockBreak.prefab", Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
		creatureCanvas.UpdateHealthBar(0, maxHp, hp);
		SoundManager.PlayOneShotAttached(breakEvent, base.transform);
		SetAnimation(rockType + "_crush", loop: false);
		destroyTimer = new FrameTimer(Util.SecondsToFrame(GetAnimDuration(rockType + "_crush")));
	}
}
