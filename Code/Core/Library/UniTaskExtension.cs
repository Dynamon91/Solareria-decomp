using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Spine.Unity;
using UnityEngine;

namespace Library;

public static class UniTaskExtension
{
	public static async UniTask WaitForSeconds(float time, CancellationToken token = default(CancellationToken))
	{
		await UniTask.Delay(new TimeSpan(0, 0, 0, 0, (int)(time * 1000f)), ignoreTimeScale: false, PlayerLoopTiming.Update, token);
	}

	public static async UniTask WaitOneFrame()
	{
		await UniTask.Yield(PlayerLoopTiming.LastFixedUpdate);
	}

	public static async UniTask WaitForFixedUpdatesOrCancel(int frameCnt, CancellationToken token = default(CancellationToken), bool ignoreTimeScale = false)
	{
		for (int i = 0; i < frameCnt; i++)
		{
			if (token.IsCancellationRequested)
			{
				break;
			}
			await WaitOneFrame();
		}
	}

	public static async UniTask WaitUntilOrCancel(Func<bool> predicate, CancellationToken cancellationToken, PlayerLoopTiming timing = PlayerLoopTiming.Update)
	{
		await UniTask.WaitUntil(() => predicate() || cancellationToken.IsCancellationRequested, timing);
	}

	public static async UniTask WaitWhileOrCancel(Func<bool> predicate, CancellationToken cancellationToken)
	{
		await UniTask.WaitWhile(() => predicate() && !cancellationToken.IsCancellationRequested);
	}

	public static async UniTask WhenAllOrCancelBy(UniTask task1, UniTask task2, Func<bool> cancelPredicate)
	{
		while ((task1.Status != UniTaskStatus.Succeeded || task2.Status != UniTaskStatus.Succeeded) && !cancelPredicate())
		{
			await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
		}
	}

	public static async UniTask WaitAsync(this Tween tween, CancellationToken cancellationToken = default(CancellationToken))
	{
		await UniTask.WaitWhile(() => tween.IsActive() && !cancellationToken.IsCancellationRequested);
	}

	public static async UniTask WaitAsync(this Sequence sequence, CancellationToken cancellationToken = default(CancellationToken))
	{
		await UniTask.WaitWhile(() => sequence.IsActive() && !cancellationToken.IsCancellationRequested);
	}

	public static async UniTask WaitAsync(this ParticleSystem particle, CancellationToken cancellationToken = default(CancellationToken))
	{
		await UniTask.WaitWhile(() => particle.isPlaying && !cancellationToken.IsCancellationRequested);
	}

	public static async UniTask WaitAnimationAsync(this SkeletonAnimation spine, CancellationToken cancellationToken = default(CancellationToken))
	{
		await UniTask.WaitUntil(() => spine.IsAnimationComplete() || cancellationToken.IsCancellationRequested);
	}

	public static async UniTask PlayAndWaitAsync(this ParticleSystem particle, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!particle.IsNull())
		{
			particle.Play();
			await UniTask.WaitWhile(() => !particle.IsNull() && (particle.isPlaying || particle.isEmitting), PlayerLoopTiming.Update, cancellationToken);
		}
	}
}
