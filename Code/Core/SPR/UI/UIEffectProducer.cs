using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LapinLibrary;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.UI;

public abstract class UIEffectProducer : MonoBehaviour
{
	[SerializeField]
	protected List<GameObject> effectObjects;

	private CancellationTokenSource effectTokenSource;

	private PlayableDirector playableDirector;

	protected PlayableDirector PlayableDirector => playableDirector ?? (playableDirector = GetComponent<PlayableDirector>());

	protected static UIManager UIManager => Game.Instance.UIManager;

	public bool IsProducing { get; private set; }

	public virtual void Initialize()
	{
		effectObjects.ForEach(delegate(GameObject obj)
		{
			obj.SetActive(value: false);
		});
	}

	public async UniTask PlayEffect()
	{
		IsProducing = true;
		effectObjects.ForEach(delegate(GameObject obj)
		{
			obj.SetActive(value: true);
		});
		TaskUtil.RefreshToken(ref effectTokenSource);
		UniTask.Awaiter startAwaiter = StartEffectAsync().GetAwaiter();
		while (!startAwaiter.IsCompleted && !effectTokenSource.IsCancellationRequested)
		{
			await UniTask.Yield();
		}
		await EndEffectAsync();
	}

	private async UniTask StartEffectAsync()
	{
		await OnStartTimeline();
		PlayableDirector.time = 0.0;
		PlayableDirector.Play();
		await UniTask.WaitUntil(() => PlayableDirector.state != PlayState.Playing);
	}

	private async UniTask EndEffectAsync()
	{
		DebugLog.GeneralLog("EndEffectAsync start");
		PlayableDirector.Stop();
		await OnEndTimeline();
		effectObjects.ForEach(delegate(GameObject obj)
		{
			obj.SetActive(value: false);
		});
		IsProducing = false;
		DebugLog.GeneralLog("EndEffectAsync end");
	}

	public void SkipEffect()
	{
		if (IsProducing)
		{
			DebugLog.GeneralLog("SkipEffectAsync");
			effectTokenSource?.Cancel();
			PlayableDirector.time = PlayableDirector.playableAsset.duration;
			PlayableDirector.Stop();
			OnSkipTimeline().GetAwaiter().OnCompleted(delegate
			{
				EndEffectAsync().Forget();
			});
		}
	}

	protected abstract UniTask OnStartTimeline();

	protected abstract UniTask OnEndTimeline();

	protected abstract UniTask OnSkipTimeline();
}
