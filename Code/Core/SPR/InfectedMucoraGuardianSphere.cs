using FMOD.Studio;
using FMODUnity;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class InfectedMucoraGuardianSphere : Projectile
{
	[SerializeField]
	private float radius;

	[SerializeField]
	private float detectRadius;

	[Header("Particle System")]
	[SerializeField]
	private ParticleSystem shot;

	[SerializeField]
	private ParticleSystem expand;

	[SerializeField]
	private ParticleSystem loop;

	[Header("SFX")]
	[SerializeField]
	private EventReference sphereEvent;

	private FrameTimer delay;

	private FrameTimer cycle;

	private FrameTimer timer;

	private EventInstance sphereInstance;

	private bool onPlatform;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(base.transform.position, radius);
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(base.transform.position, detectRadius);
	}

	protected override void OnLaunch()
	{
		sphereInstance = SoundManager.PlayOneShotAttached(sphereEvent, base.transform);
	}

	public override void InitializeBeforeUse()
	{
		base.InitializeBeforeUse();
		ReturnToPoolControl = true;
		onPlatform = false;
	}

	public override void ReturnToPool()
	{
		onPlatform = false;
		base.ReturnToPool();
		sphereInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
		sphereInstance.release();
	}

	public void PlayEffect()
	{
		shot.Play(withChildren: true);
		expand.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
		loop.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
	}

	public void SetCycle(int delayFrame, int cycleFrame, int duration)
	{
		delay = new FrameTimer(delayFrame);
		cycle = new FrameTimer(cycleFrame);
		timer = new FrameTimer(duration);
	}

	protected override void OnPlatform()
	{
		onPlatform = true;
		Speed = 0f;
		SetVelocity();
		shot.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
		expand.Play(withChildren: true);
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if (!onPlatform)
		{
			return;
		}
		if (!delay.IsDone)
		{
			delay.Tick();
			return;
		}
		if (!loop.isPlaying)
		{
			loop.Play(withChildren: true);
		}
		if (cycle.IsDone)
		{
			AttackUtility.AttackCircle(base.transform.position, radius, new HitInfo(this, "Expand"), LayerUtility.PlayerLayer);
			cycle.Reset();
		}
		if (timer.IsDone)
		{
			ReturnToPool();
		}
		cycle.Tick();
		timer.Tick();
	}
}
