using FMODUnity;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class IswFireProjectile : Projectile
{
	[Header("투사체 설정")]
	[SerializeField]
	private int duration;

	[Header("폭발 반경")]
	[SerializeField]
	private float explosionRadius;

	[Header("References")]
	[SerializeField]
	private ParticleSystem particle;

	[SerializeField]
	private EventReference impactEvent;

	private FrameTimer timer;

	public override void InitializeBeforeUse()
	{
		base.InitializeBeforeUse();
		ReturnToPoolControl = true;
		particle.Play(withChildren: true);
	}

	public override void ReturnToPool()
	{
		timer = null;
		base.ReturnToPool();
	}

	public void Initialize(Vector2 direction, float speed)
	{
		Launch(direction, speed);
	}

	private void Explode()
	{
		Speed = 0f;
		timer = new FrameTimer(duration);
		particle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
		Projectile.PoolManager.GetObject<PoolingParticle>("Infected_Spirit_Fire_attack01_Hit.prefab", base.transform.position).Play();
	}

	protected override void OnHit()
	{
		Explode();
	}

	protected override void OnParry()
	{
		Explode();
	}

	protected override void OnPlatform()
	{
		particle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
		ReturnToPool();
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if (timer != null && timer.IsDone)
		{
			timer = null;
			AttackUtility.AttackCircle(base.transform.position, explosionRadius, new HitInfo(this, "Explosion"), LayerUtility.PlayerLayer);
			Game.Instance.SoundManager.PlayOneShotAttached(impactEvent, base.transform);
			ReturnToPool();
		}
		timer?.Tick();
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(base.transform.position, explosionRadius);
	}
}
