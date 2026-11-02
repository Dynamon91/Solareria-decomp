using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Timeline;

namespace SPR.Field;

public class ItemBoxRare : ItemBox
{
	[Header("Rare")]
	[SerializeField]
	private TimelineAsset burnTimeline;

	[SerializeField]
	private Light2D jemLight;

	protected override void OnCompletePush()
	{
		base.PlayableDirector.playableAsset = burnTimeline;
		base.PlayableDirector.Play();
	}

	protected override async UniTask PlayPushAsync(CancellationToken token)
	{
		Sequence seq = GetJemLightSequence();
		seq.Play();
		await base.PlayPushAsync(token);
		if (token.IsCancellationRequested)
		{
			seq.Kill();
			jemLight.DOKill();
			jemLight.transform.DOKill();
		}
	}

	protected override async UniTask ResetAsync(float duration)
	{
		jemLight.DoLightIntensity(0f, duration);
		jemLight.DoLightVolumeIntensity(0f, duration);
		jemLight.transform.DOScale(Vector3.zero, duration);
		await base.ResetAsync(duration);
	}

	private Sequence GetJemLightSequence()
	{
		float duration = Util.FrameToSeconds(45f);
		Sequence sequence = DOTween.Sequence();
		sequence.AppendCallback(delegate
		{
			jemLight.intensity = 0f;
			jemLight.volumeIntensity = 0f;
			jemLight.transform.localScale = Vector3.zero;
		});
		sequence.AppendInterval(Util.FrameToSeconds(115f)).Append(jemLight.DoLightIntensity(2f, duration)).Join(jemLight.DoLightVolumeIntensity(0.3f, duration))
			.Join(jemLight.transform.DOScale(Vector3.one * 1.5f, duration));
		return sequence;
	}
}
