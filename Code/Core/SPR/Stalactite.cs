using Cysharp.Threading.Tasks;
using FMODUnity;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class Stalactite : SpineObject, IPoolableObject
{
	[SerializeField]
	private float explosiveWaitSeconds;

	[SerializeField]
	private Zone explosionZone;

	[Header("SFX")]
	[SerializeField]
	private EventReference installEvent;

	[SerializeField]
	private EventReference explodeEvent;

	private Rigidbody2D rb2D;

	private Rigidbody2D Rb2D => rb2D ?? (rb2D = GetComponent<Rigidbody2D>());

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public string PrefabKey => "Stalactite.prefab";

	public GameObject GameObject => base.gameObject;

	public override string Name => "Stalactite";

	public override Direction FacingDirection => Direction.Right;

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		ObjectPoolManager objectPoolManager = Game.Instance.ObjectPoolManager;
		PoolableTimeLimitObject poolableTimeLimitObject = objectPoolManager.GetObject<PoolableTimeLimitObject>("VFX/Stalactite_Explosion.prefab", base.transform.position);
		poolableTimeLimitObject.Initialize(Util.GetParticlesMaxLifeTime(poolableTimeLimitObject));
		RemoveAnimation();
		objectPoolManager.Return(this);
	}

	public async UniTaskVoid Explode()
	{
		SetAnimation("throw", loop: false);
		SoundManager.PlayOneShotAttached(installEvent, base.transform);
		await UniTask.WaitForSeconds(explosiveWaitSeconds);
		AttackUtility.AttackBox((Vector2)base.transform.position + explosionZone.Center, explosionZone.Size, new HitInfo(this, string.Empty), LayerUtility.EnemyDefaultAttackLayer);
		SoundManager.PlayOneShotAttached(explodeEvent, base.transform);
		ReturnToPool();
	}

	protected override void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
		Gizmos.DrawCube(base.transform.position + (Vector3)explosionZone.Center, explosionZone.Size);
	}
}
