using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using LapinLibrary;
using Library;
using UnityEngine;

namespace SPR;

public class BulletTimeHandler
{
	private readonly CinemachineImpulseSource impulseSource;

	private TimeScaleEventParameter parameter;

	private CinemachineBrain brain;

	private CinemachineVirtualCamera originVcam;

	private CinemachineVirtualCamera bulletVcam;

	private float originOrthoSize;

	private float originDeltaTime;

	private CancellationTokenSource tokenSource;

	private Tween timeScaleTween;

	private int predelayFrame;

	private int actionFrame;

	private int resetDurationFrame;

	private float changedTimeScale;

	private bool impulse;

	public bool IsPlaying { get; private set; }

	public BulletTimeHandler(TimeScaleEventParameter parameter, CinemachineImpulseSource impulseSource)
	{
		this.parameter = parameter;
		this.impulseSource = impulseSource;
		originDeltaTime = Time.fixedDeltaTime;
		predelayFrame = parameter.PredelayFrame;
		actionFrame = parameter.ChangedScaleFrame;
		resetDurationFrame = parameter.ResetFrame;
		changedTimeScale = parameter.ChangedScale;
		impulse = true;
	}

	public BulletTimeHandler(int predelay, int action, int resetDurationFrame, float changedScale, bool impulse)
	{
		predelayFrame = predelay;
		actionFrame = action;
		this.resetDurationFrame = resetDurationFrame;
		changedTimeScale = changedScale;
		this.impulse = impulse;
	}

	public void PlayBulletTime()
	{
		if (timeScaleTween != null && timeScaleTween.IsActive())
		{
			timeScaleTween.Kill();
		}
		PlayAsync(TaskUtil.RefreshToken(ref tokenSource)).Forget();
	}

	public void Cancel()
	{
		tokenSource?.Cancel();
	}

	private async UniTask PlayAsync(CancellationToken token)
	{
		IsPlaying = true;
		await UniTask.WaitForSeconds(Util.FrameToSeconds(predelayFrame), ignoreTimeScale: true, PlayerLoopTiming.Update, token);
		ChangeTimeScale(changedTimeScale, 0f);
		float duration = Util.FrameToSeconds(actionFrame);
		if (impulse)
		{
			GenerateImpulse(duration);
		}
		await UniTask.WaitForSeconds(duration, ignoreTimeScale: true, PlayerLoopTiming.Update, token);
		await ChangeTimeScale(1f, Util.FrameToSeconds(resetDurationFrame)).WaitAsync(token);
		IsPlaying = false;
	}

	private void Initialize()
	{
		brain = Camera.main.GetComponent<CinemachineBrain>();
		originVcam = brain.ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>();
		originOrthoSize = originVcam.m_Lens.OrthographicSize;
	}

	private void GenerateImpulse(float duration)
	{
		impulseSource.m_ImpulseDefinition.m_ImpulseDuration = duration;
		impulseSource.GenerateImpulse();
	}

	private Tween ChangeTimeScale(float scale, float duration)
	{
		TweenerCore<float, float, FloatOptions> tweenerCore = DOTween.To(() => Time.timeScale, Game.SetTimeScale, scale, duration).OnComplete(delegate
		{
			Debug.Log($"Time Scale:{Time.timeScale}, Time:{Time.time}, Delta:{Time.deltaTime}");
		});
		tweenerCore.Play();
		return tweenerCore;
	}

	private void CameraEffect(bool active)
	{
		float endValue = ((active && parameter.CameraZoomSize > 0) ? ((float)parameter.CameraZoomSize) : originOrthoSize);
		float endValue2 = (active ? GetRandomAngle() : 0f);
		float duration = (active ? Util.FrameToSeconds(parameter.PredelayFrame) : Util.FrameToSeconds(parameter.ResetFrame));
		Sequence sequence = DOTween.Sequence();
		sequence.Append(DOTween.To(() => bulletVcam.m_Lens.OrthographicSize, delegate(float x)
		{
			bulletVcam.m_Lens.OrthographicSize = x;
		}, endValue, duration)).Join(DOTween.To(() => bulletVcam.m_Lens.Dutch, delegate(float x)
		{
			bulletVcam.m_Lens.Dutch = x;
		}, endValue2, duration));
		sequence.Play();
	}

	private float GetRandomAngle()
	{
		int num = ((Random.Range(0, 1) != 0) ? 1 : (-1));
		return Random.Range(parameter.CameraMinAngle, parameter.CameraMaxAngle) * num;
	}
}
