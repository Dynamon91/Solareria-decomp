using System.Threading;
using Cysharp.Threading.Tasks;
using FMODUnity;
using LapinLibrary;
using Library;
using UnityEngine;

namespace SPR;

public class RoyalGuardWaterBomb : Projectile
{
	[SerializeField]
	private EventReference waitEvent;

	[SerializeField]
	private EventReference shootEvent;

	[SerializeField]
	private EventReference splashEvent;

	private CancellationTokenSource cancellationTokenSource;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public override void ReturnToPool()
	{
		Speed = 0f;
		SetVelocity();
		cancellationTokenSource?.Cancel();
		base.ReturnToPool();
	}

	public async UniTaskVoid Shoot(float speed, int waitFrame)
	{
		EnableVFX("cast");
		SoundManager.PlayOneShotAttached(waitEvent, base.transform);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(waitFrame, TaskUtil.RefreshToken(ref cancellationTokenSource));
		SoundManager.PlayOneShotAttached(shootEvent, base.transform);
		PlayerMover mover = Game.Instance.Player.mover;
		Speed = speed;
		MoveDirection = (mover.VisualCenter - (Vector2)base.transform.position).normalized;
		SetVelocity();
	}

	protected override void OnFixedUpdate()
	{
		OverlapCenter = base.transform.position;
		base.OverlapSize = base.Box2D.size;
	}

	protected override void OnHit()
	{
		Projectile.PoolManager.GetObject<PoolingParticle>("VFX/WaterDragon_WaterBomb_Hit.prefab", base.transform.position).Play();
		SoundManager.PlayOneShotAttached(splashEvent, base.transform);
	}

	protected override void OnPlatform()
	{
		Projectile.PoolManager.GetObject<PoolingParticle>("VFX/WaterDragon_WaterBomb_Hit.prefab", base.transform.position).Play();
		SoundManager.PlayOneShotAttached(splashEvent, base.transform);
	}
}
