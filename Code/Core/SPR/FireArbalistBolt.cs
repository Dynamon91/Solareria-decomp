using System;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class FireArbalistBolt : Projectile
{
	public enum BoltType
	{
		Normal,
		Enhanced,
		Explosive
	}

	[Header("Bolt")]
	[SerializeField]
	private BoltType type;

	[SerializeField]
	private GameObject[] activeList;

	[SerializeField]
	private float returnDelay;

	[Header("Particle")]
	[SerializeField]
	private string shootParticleAddress;

	[SerializeField]
	private ParticleSystem sign;

	private FrameTimer explosiveTimer;

	private EventReference explodeEvent;

	private EventInstance explodeInstance;

	private CircleCollider2D circle2D;

	private CircleCollider2D Circle2D => circle2D ?? (circle2D = GetComponent<CircleCollider2D>());

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public override void InitializeBeforeUse()
	{
		base.InitializeBeforeUse();
		SetActiveObjects(value: true);
		BoltType boltType = type;
		if (boltType == BoltType.Enhanced || boltType == BoltType.Explosive)
		{
			ReturnToPoolControl = true;
		}
		explosiveTimer = null;
		if (!shootParticleAddress.IsNullOrEmpty())
		{
			Projectile.PoolManager.GetObject<PoolingParticle>(shootParticleAddress, base.transform.position, base.transform.rotation).Play();
		}
	}

	public override void ReturnToPool()
	{
		ReturnToPoolAsync().Forget();
	}

	public void SetExplosiveTimer(int duration, EventReference delayEvent)
	{
		SetActiveObjects(value: false);
		Projectile.PoolManager.GetObject<PoolingParticle>("FireArbalist_Bolt_Stuck_Piece.prefab", base.transform.position, base.transform.rotation).Play();
		Projectile.PoolManager.GetObject<PoolingParticle>("FireArbalist_Bolt_Stuck_Spawner.prefab", base.transform.position, base.transform.rotation).Play();
		sign.Play(withChildren: true);
		explosiveTimer = new FrameTimer(duration);
		if (!explodeInstance.isValid())
		{
			SoundManager.PlayOneShotAttached(delayEvent, base.transform);
		}
	}

	public void SetExplodeEvent(EventReference eventReference)
	{
		explodeEvent = eventReference;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if (explosiveTimer != null)
		{
			explosiveTimer.Tick();
			if (explosiveTimer.IsDone)
			{
				Explosion();
				explosiveTimer = null;
			}
		}
	}

	protected override void OnHit()
	{
		switch (type)
		{
		case BoltType.Normal:
			Speed = 0f;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case BoltType.Enhanced:
		case BoltType.Explosive:
			break;
		}
	}

	protected override void OnPlatform()
	{
		if (type == BoltType.Enhanced)
		{
			ReturnToPool();
		}
	}

	protected override void OnParry()
	{
		if (type == BoltType.Explosive)
		{
			Explosion();
			ReturnToPool();
		}
	}

	private void Explosion()
	{
		AttackUtility.AttackCircle(base.Position, Circle2D.radius, new HitInfo(this, "Explosion"), (int)LayerUtility.PlayerLayer | (int)LayerUtility.MonsterLayer);
		Projectile.PoolManager.GetObject<PoolingParticle>("Fire Arbalist Bolt_Explosive_Explosion.prefab", base.Position).Play();
		explodeInstance = SoundManager.PlayOneShotAttached(explodeEvent, base.transform);
		ReturnToPool();
	}

	private async UniTaskVoid ReturnToPoolAsync()
	{
		SetActiveObjects(value: false);
		explodeInstance.release();
		await UniTask.WaitForSeconds(returnDelay);
		base.ReturnToPool();
	}

	private void SetActiveObjects(bool value)
	{
		GameObject[] array = activeList;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value);
		}
	}
}
