using Cysharp.Threading.Tasks;
using FMODUnity;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class WebbedLizardBubble : Projectile, IDamageable
{
	[SerializeField]
	private float maxHealth;

	[SerializeField]
	private EventReference popEvent;

	private FrameTimer timer;

	private float health;

	public override string Name => "WebbedLizardBubble";

	public HitType HitType => HitType.Object;

	public override string PrefabKey => "WebbedLizardBubble.prefab";

	public override void InitializeBeforeUse()
	{
		base.InitializeBeforeUse();
		health = maxHealth;
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		if (health <= 0f)
		{
			return false;
		}
		health -= hitInfo.Damage;
		if (health <= 0f)
		{
			Pop().Forget();
			return true;
		}
		return true;
	}

	public void Initialize(Vector2 direction, float speed, int duration)
	{
		MoveDirection = direction;
		Speed = speed;
		timer = new FrameTimer(duration);
		SetVelocity();
		SetAnimation("shot", loop: false);
		AddAnimation("idle");
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if (timer != null)
		{
			if (timer.IsDone)
			{
				Pop().Forget();
			}
			else
			{
				timer.Tick();
			}
		}
	}

	protected override void OnHit()
	{
		OnPop();
	}

	protected override void OnPlatform()
	{
		OnPop();
	}

	private async UniTaskVoid Pop()
	{
		OnPop();
		await UniTask.Yield();
		ReturnToPool();
	}

	private void OnPop()
	{
		timer = null;
		Projectile.PoolManager.GetObject<PoolingParticle>("VFX/WaterDragon_WaterBomb_Hit.prefab", base.transform.position).Play();
		Game.Instance.SoundManager.PlayOneShotAttached(popEvent, base.transform);
	}
}
