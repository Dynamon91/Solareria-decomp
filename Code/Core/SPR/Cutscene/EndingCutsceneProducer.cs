using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Library;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SPR.Cutscene;

public class EndingCutsceneProducer : CutsceneProducer
{
	private const int WakeUpStateIndex = 0;

	private const int AfterWakeUpStateIndex = 1;

	private const int TalkStateIndex = 2;

	private const int PastStateIndex = 3;

	private const int CollapseStateIndex = 4;

	private const int ExecuteStateIndex = 5;

	private const int ExplodeStateIndex = 6;

	[Header("Timeline Assets")]
	[SerializeField]
	private PlayableAsset wakeUpTimeline;

	[SerializeField]
	private PlayableAsset talkTimeline;

	[SerializeField]
	private PlayableAsset pastTimeline;

	[SerializeField]
	private PlayableAsset pastImageTimeline;

	[SerializeField]
	private PlayableAsset collapseTimeline;

	[SerializeField]
	private PlayableAsset executeTimeline;

	[SerializeField]
	private PlayableAsset explodeTimeline;

	[SerializeField]
	private PlayableAsset normalTimeline;

	[Header("Wake Up")]
	[SerializeField]
	private CinemachineVirtualCamera wakeUpEndCam;

	[SerializeField]
	private Transform wakeUpPosition;

	[SerializeField]
	private Transform walkEffectParent;

	[SerializeField]
	private Transform walkEffectWave;

	[SerializeField]
	private Transform walkEffectFire;

	[Header("Talk")]
	[SerializeField]
	private CinemachineVirtualCamera talkCam;

	[SerializeField]
	private Transform talkPosition;

	[SerializeField]
	private ParticleSystem kingEmberEffect;

	[Header("Past")]
	[Header("Collapse")]
	[Header("Execute")]
	[Header("Explode")]
	[Header("References")]
	[SerializeField]
	private Transform playerFigure;

	[SerializeField]
	private CutsceneTrigger cutsceneTrigger;

	[SerializeField]
	private Volume ppVolume;

	[SerializeField]
	private GameObject backgroundEnvironments;

	[Header("Values")]
	[SerializeField]
	private float creditDelay;

	private Transform vfxOrigin;

	private PlayableAsset sideTimeline;

	private PlayableAsset originTimeline;

	private float pauseTime;

	private ColorAdjustments colorAdjustments;

	private bool shiftNarration;

	private NullControllable nullControllable = new NullControllable("block for player control");

	protected override List<int> UnskippableCutscenes => new List<int> { 0, 2, 3, 4, 5, 6 };

	private static bool IsTrueEnding => Trigger.CheckTrigger("Trigger_IsTrueEnding");

	private static Camera MainCamera => Camera.main;

	private static ObjectPoolManager ObjectPoolManager => Game.Instance.ObjectPoolManager;

	public override void Control()
	{
		base.Control();
		if (!shiftNarration)
		{
			shiftNarration = base.PlayableDirector.playableAsset == pastImageTimeline && CutsceneProducer.InputManager.confirmDown;
		}
	}

	protected override void SetProducerState(int state)
	{
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 0:
			base.FieldController.ChangeAreaTo(base.MyArea);
			CutsceneProducer.SoundManager.SetTrueEnding(IsTrueEnding);
			CutsceneProducer.SoundManager.SetEndingBgmProgress(0);
			base.PlayableDirector.playableAsset = wakeUpTimeline;
			playerFigure.SynchronizePositionDirection(wakeUpPosition);
			CutsceneProducer.Player.transform.SynchronizePositionDirection(wakeUpPosition);
			base.MyArea.Vcam.GetComponent<VcamZoomBlender>().SetLensSize();
			CutsceneProducer.FadeCanvas.FadeIn(2f, 0f, Ease.Linear, token).Forget();
			SetPlatformVisible(value: false);
			break;
		case 2:
			base.PlayableDirector.playableAsset = talkTimeline;
			await CutsceneProducer.Player.MoveXPositionAsync(talkPosition.position.x, isRunning: false);
			await BlendFieldCamToCutsceneStartCamAsync(talkCam);
			playerFigure.SynchronizePositionDirection(CutsceneProducer.Player.mover.transform);
			SwitchToFakeSpine(CutsceneProducer.Player.mover, playerFigure, playerFigure.GetComponentInChildren<SkeletonAnimation>());
			break;
		case 3:
			base.PlayableDirector.playableAsset = pastTimeline;
			CutsceneProducer.SoundManager.SetEndingBgmProgress(1);
			sideTimeline = pastImageTimeline;
			break;
		case 4:
			base.PlayableDirector.playableAsset = collapseTimeline;
			CutsceneProducer.SoundManager.SetEndingBgmProgress(4);
			CutsceneProducer.SoundManager.PlayAMB("EndingEarthquake");
			break;
		case 5:
			base.PlayableDirector.playableAsset = executeTimeline;
			break;
		case 6:
			base.PlayableDirector.playableAsset = (IsTrueEnding ? explodeTimeline : normalTimeline);
			break;
		case 1:
			break;
		}
	}

	protected override void OnEndTimeline(int state)
	{
		switch (state)
		{
		case 0:
			base.MyArea.Vcam.transform.SynchronizePositionDirection(wakeUpEndCam.transform);
			SynchronizeEndVcamAsync(wakeUpEndCam).Forget();
			CutsceneProducer.Player.SetEndingCutsceneProducer(this);
			CutsceneProducer.Player.mover.SynchronizeAnimation(playerFigure.GetComponentInChildren<SkeletonAnimation>(includeInactive: true));
			SynchronizePlayer(playerFigure);
			CutsceneProducer.Field.ChangeAreaTo(base.MyArea);
			cutsceneTrigger.OnTriggered += delegate
			{
				PlayTimelineAsync(2, blockControl: true).Forget();
			};
			break;
		case 2:
			PlayTimelineAfterEnd(IsTrueEnding ? 3 : 4, blocControl: true, hideCanvas: true);
			CutsceneProducer.InputManager.StackControlTarget(nullControllable).Forget();
			break;
		case 3:
			PlayTimelineAfterEnd(4, blocControl: true, hideCanvas: true);
			break;
		case 4:
			PlayTimelineAfterEnd(5, blocControl: true, hideCanvas: true);
			break;
		case 5:
			PlayTimelineAfterEnd(6, blocControl: true, hideCanvas: true);
			break;
		case 6:
			LoadCreditScene().Forget();
			break;
		case 1:
			break;
		}
	}

	protected override void OnSkipTimeline(int state)
	{
		base.PlayableDirector.time = base.PlayableDirector.duration;
		base.PlayableDirector.Evaluate();
		switch (state)
		{
		case 2:
			StopKingEmber();
			break;
		case 4:
			StopEndingSpace();
			break;
		}
	}

	public override bool ShouldLoop(int index)
	{
		switch (base.CurrentState)
		{
		case 2:
		case 4:
			return CutsceneProducer.IsTalking;
		case 3:
			return (index == 0) ? CutsceneProducer.IsTalking : (!shiftNarration);
		default:
			return false;
		}
	}

	public override void EnterLoop(int index)
	{
		switch (base.CurrentState)
		{
		case 2:
		case 4:
			StartScenarioInsideCutscene(index);
			break;
		case 3:
			if (index == 0)
			{
				StartScenarioInsideCutscene(index);
				break;
			}
			shiftNarration = false;
			CutsceneProducer.FieldCanvas.NarrationBoard.EnableControlGuide(enable: true);
			break;
		}
	}

	public override void ExitLoop(int index)
	{
		switch (base.CurrentState)
		{
		case 3:
			if (index != 0)
			{
				shiftNarration = false;
				CutsceneProducer.FieldCanvas.NarrationBoard.EnableControlGuide(enable: false);
			}
			break;
		case 4:
			_ = 1;
			break;
		}
	}

	public void StopKingEmber()
	{
		kingEmberEffect.Stop();
	}

	public void PlaySideTimeline()
	{
		if (BasicExtension.IsNull(sideTimeline))
		{
			DebugLog.GeneralLog("sideTimeline이 존재하지 않으므로 교체하지 않습니다!");
			return;
		}
		pauseTime = (float)(int)(base.PlayableDirector.time * 60.0) / 60f;
		originTimeline = base.PlayableDirector.playableAsset;
		SwitchTimeline(sideTimeline, 0f);
		CutsceneProducer.SoundManager.SetEndingBgmProgress(2);
	}

	public void ResumeOriginTimeline()
	{
		SwitchTimeline(originTimeline, pauseTime);
		pauseTime = 0f;
		sideTimeline = null;
		originTimeline = null;
		CutsceneProducer.SoundManager.SetEndingBgmProgress(3);
	}

	public void StopEndingSpace()
	{
		SetPlatformVisible(value: true);
	}

	public void SetPPColorFilter(bool value)
	{
		if (BasicExtension.IsNull(colorAdjustments))
		{
			ppVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments);
		}
		colorAdjustments.colorFilter.overrideState = value;
	}

	public void SetFadeAMBVolume(float value)
	{
		CutsceneProducer.SoundManager.SetFadeAMBVolume(value);
	}

	public void PlayEpilogueBGM()
	{
		CutsceneProducer.SoundManager.PlayBGM("EndingEpilogue");
		CutsceneProducer.SoundManager.SetTrueEnding(IsTrueEnding);
	}

	public void SetEndingBGMClimax()
	{
		CutsceneProducer.SoundManager.SetEndingBgmProgress(5);
	}

	private void SetPlatformVisible(bool value)
	{
		MainCamera.cullingMask = (value ? (MainCamera.cullingMask | (int)LayerUtility.PlatformLayer) : (MainCamera.cullingMask & ~(int)LayerUtility.PlatformLayer));
		backgroundEnvironments.SetActive(value);
	}

	public void PlayWalkingEffect(bool playFire = false)
	{
		walkEffectParent.SynchronizePositionDirection(CutsceneProducer.Player.transform);
		PoolingParticle poolingParticle = ObjectPoolManager.GetObject<PoolingParticle>("Ending_Tot_Footstep_Wave.prefab", walkEffectParent);
		poolingParticle.transform.SynchronizeLocalPositionDirection(walkEffectWave);
		poolingParticle.Play();
		if (playFire)
		{
			PoolingParticle poolingParticle2 = ObjectPoolManager.GetObject<PoolingParticle>("Ending_Tot_Footstep_Fire.prefab", walkEffectParent);
			poolingParticle2.transform.SynchronizeLocalPositionDirection(walkEffectFire);
			poolingParticle2.Play();
		}
	}

	private void PlayTimelineAfterEnd(int state, bool blocControl, bool hideCanvas)
	{
		UniTask.WaitWhile(() => IsProducing).GetAwaiter().OnCompleted(delegate
		{
			PlayTimelineAsync(state, blocControl, hideCanvas).Forget();
		});
	}

	private async UniTask LoadCreditScene()
	{
		await UniTask.WaitForSeconds(creditDelay);
		Game.Instance.LoadFieldAsync(SceneDataModel.CreditModel()).GetAwaiter().OnCompleted(delegate
		{
			CutsceneProducer.InputManager.RemoveControlTarget(nullControllable).Forget();
		});
	}

	private void SwitchTimeline(PlayableAsset timeline, float time)
	{
		base.PlayableDirector.Pause();
		base.PlayableDirector.playableAsset = timeline;
		base.PlayableDirector.time = time;
		base.PlayableDirector.Evaluate();
		base.PlayableDirector.Play();
	}
}
