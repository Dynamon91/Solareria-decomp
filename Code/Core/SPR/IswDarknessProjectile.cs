using FMODUnity;
using UnityEngine;

namespace SPR;

public class IswDarknessProjectile : Projectile
{
	[SerializeField]
	private int smokeDuration;

	[SerializeField]
	private int smokeAttackCycle;

	[SerializeField]
	private EventReference smokeEvent;

	public void Initialize(Vector2 direction, float speed)
	{
		Launch(direction, speed);
	}

	protected override void OnHit()
	{
		Smoke();
	}

	protected override void OnPlatform()
	{
		Smoke();
	}

	private void Smoke()
	{
		Projectile.PoolManager.GetObject<IswDarknessSmoke>("Infected Spirit Warrior Darkness Smoke.prefab", VisualCenter).HandleOverlap(smokeDuration, smokeAttackCycle).Forget();
		Game.Instance.SoundManager.PlayOneShotAttached(smokeEvent, base.transform);
	}
}
