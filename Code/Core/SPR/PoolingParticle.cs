using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;

namespace SPR;

public class PoolingParticle : PoolingObject
{
	[SerializeField]
	private ParticleSystem particle;

	[SerializeField]
	private ParticleSystem[] particles;

	private CancellationTokenSource cts;

	public ParticleSystem ParticleSystem => particle;

	public IReadOnlyList<ParticleSystem> Particles => particles;

	public override void InitializeBeforeUse()
	{
		base.InitializeBeforeUse();
		cts = new CancellationTokenSource();
	}

	public override void ReturnToPool()
	{
		base.ReturnToPool();
		cts.Cancel();
	}

	public void Play()
	{
		if (!particle.IsNull())
		{
			particle.Play();
		}
		ParticleSystem[] array = particles;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Play();
		}
		WaitUntilComplete().Forget();
	}

	public void Stop(bool withChildren)
	{
		if (!particle.IsNull())
		{
			particle.Stop(withChildren);
		}
		ParticleSystem[] array = particles;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Stop();
		}
	}

	public void Stop(bool withChildren, ParticleSystemStopBehavior stopBehavior)
	{
		particle.Stop(withChildren, stopBehavior);
	}

	public async UniTaskVoid StopWithDestroy(bool withChildren)
	{
		if (!particle.IsNull())
		{
			particle.Stop(withChildren);
		}
		ParticleSystem[] array = particles;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Stop();
		}
		if (!particle.IsNull())
		{
			await UniTask.WaitUntil(() => !particle.IsAlive(withChildren: true));
		}
		if (particles.Length != 0)
		{
			await UniTask.WaitWhile(() => particles.Any((ParticleSystem p) => p.IsAlive(withChildren: true)));
		}
		Object.Destroy(base.gameObject);
	}

	public bool IsAlive()
	{
		if (!particle.IsAlive(withChildren: true))
		{
			return particles.All((ParticleSystem p) => p.IsAlive(withChildren: true));
		}
		return true;
	}

	private async UniTaskVoid WaitUntilComplete()
	{
		if (!particle.IsNull())
		{
			await UniTask.WaitUntil(() => !particle.IsAlive(withChildren: true), PlayerLoopTiming.Update, cts.Token);
		}
		if (particles.Length != 0)
		{
			await UniTask.WaitWhile(() => particles.Any((ParticleSystem p) => p.IsAlive(withChildren: true)), PlayerLoopTiming.Update, cts.Token);
		}
		ReturnToPool();
	}

	private void OnDestroy()
	{
		cts?.Cancel();
	}
}
