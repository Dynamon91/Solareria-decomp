using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LapinLibrary;
using Library;
using SPR.Field;
using Spine.Unity;
using Timeline.Samples;
using UnityEngine;
using UnityEngine.Timeline;

namespace SPR.Cutscene;

public class ImahenCutsceneProducer : CutsceneProducer
{
	private const int OpeningStateIndex = 0;

	public const int AfterOpeningStateIndex = 1;

	public const int EndingStateIndex = 2;

	private const int AfterEndingStateIndex = 3;

	public const int EndingFailedStateIndex = 4;

	private const int AfterEndingFailedStateIndex = 5;

	public const int EndingSuccessStateIndex = 6;

	private const int AfterEndingSuccessStateIndex = 7;

	[Header("Timeline Asset")]
	[SerializeField]
	private TimelineAsset openingTimeline;

	[SerializeField]
	private TimelineAsset endingTimeline;

	[SerializeField]
	private TimelineAsset endingSuccessTimeline;

	[SerializeField]
	private TimelineAsset endingFailedTimeline;

	[Header("Opening")]
	[SerializeField]
	private CinemachineVirtualCamera openingStartCam;

	[SerializeField]
	private CinemachineVirtualCamera openingFinishCam;

	[SerializeField]
	private Transform openingStartPosition;

	[Header("Retry")]
	[SerializeField]
	private Transform chaserStartPositionRetry;

	[SerializeField]
	private Transform chaserEndPositionRetry;

	[SerializeField]
	private CutsceneTrigger cutsceneTriggerRetry;

	[Header("Ending")]
	[SerializeField]
	private CinemachineVirtualCamera endingStartCam;

	[SerializeField]
	private CinemachineVirtualCamera endingFinishCam;

	[SerializeField]
	private CinemachineVirtualCamera endingSuccessFinishCam;

	[SerializeField]
	private Transform endingStartPosition;

	[SerializeField]
	private Transform endingEndPosition;

	[SerializeField]
	private float failChaserPositionX;

	[Header("Chaser")]
	[SerializeField]
	private Transform chaserObject;

	[SerializeField]
	private Transform failedChaserStartPosition;

	[SerializeField]
	private Transform failedChaserFinishPosition;

	[SerializeField]
	private float chaserMoveDuration;

	[Header("Reset")]
	[SerializeField]
	private Transform resetPlayerPosition;

	[SerializeField]
	private Transform resetChaserPosition;

	[SerializeField]
	private SkeletonAnimation leverSpine;

	[SerializeField]
	private SpriteRenderer leverWhite;

	[Header("References")]
	[SerializeField]
	private Transform playerFigure;

	[SerializeField]
	private Transform imahenFigure;

	[SerializeField]
	private Transform chasaFigure;

	[SerializeField]
	private CutsceneTrigger cutsceneTriggerOpening;

	[SerializeField]
	private CutsceneInteraction cutsceneInteraction;

	[SerializeField]
	private ImahenButtonAction buttonAction;

	[SerializeField]
	private GameObject transparentDoor;

	[SerializeField]
	private List<ParticleSystem> chaserParticles;

	[SerializeField]
	private Transform[] chaserInitialPositions;

	[SerializeField]
	private SubArea bossArea;

	[SerializeField]
	private ImahenChaserBreakable[] imahenChaserBreakables;

	[SerializeField]
	private ParticleSystem hologramParticle;

	private SkeletonAnimation imahenSpine;

	private SkeletonAnimation playerFigureSpine;

	private ImahenChaser imahenChaser;

	private ImahenSecureCutsceneProducer secureCutscene;

	private CancellationTokenSource chaserFailTokenSource;

	private SkeletonAnimation ImahenSpine => imahenSpine ?? (imahenSpine = imahenFigure.GetComponentInChildren<SkeletonAnimation>());

	private SkeletonAnimation PlayerFigureSpine => playerFigureSpine ?? (playerFigureSpine = playerFigure.GetComponentInChildren<SkeletonAnimation>(includeInactive: true));

	public ImahenChaser ImahenChaser => imahenChaser ?? (imahenChaser = chaserObject.GetComponent<ImahenChaser>());

	public bool IsResetting { get; private set; }

	private void OnDestroy()
	{
		if (IsProducing)
		{
			BreakTimeline();
		}
		PlayAlert(value: false);
	}

	protected override void SetProducerState(int state)
	{
		if ((object)secureCutscene == null)
		{
			secureCutscene = base.transform.parent.GetComponentInChildren<ImahenSecureCutsceneProducer>(includeInactive: true);
		}
		switch (state)
		{
		case 0:
			SetOpeningState();
			SetEndingState();
			break;
		case 1:
			imahenFigure.gameObject.SetActive(value: true);
			hologramParticle.gameObject.SetActive(value: true);
			hologramParticle.Play();
			SetRetryState();
			SetEndingState();
			break;
		case 2:
		case 3:
		case 4:
		case 5:
			SetRetryState();
			SetEndingState();
			break;
		case 7:
			ImahenChaser.Stop();
			transparentDoor.SetActive(value: true);
			cutsceneInteraction.gameObject.SetActive(value: false);
			base.PlayableDirector.playableAsset = endingSuccessTimeline;
			base.PlayableDirector.time = endingSuccessTimeline.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.Stop();
			SynchronizeEndVcamAsync(endingSuccessFinishCam).Forget();
			secureCutscene.SetInteractionActive(value: true);
			break;
		case 6:
			break;
		}
	}

	private void SetOpeningState()
	{
		cutsceneTriggerOpening.OnTriggered += delegate
		{
			PlayTimelineAsync(0, blockControl: true).Forget();
		};
	}

	private void SetRetryState()
	{
		cutsceneTriggerRetry.OnTriggered += delegate
		{
			ActivateRetry().Forget();
		};
	}

	private void SetEndingState()
	{
		cutsceneInteraction.AwakeObject(Game.Instance.SaveData);
		cutsceneInteraction.gameObject.SetActive(value: true);
		UniTask.WaitUntil(() => cutsceneInteraction.IsInteractionCompleted()).GetAwaiter().OnCompleted(delegate
		{
			PlayTimelineAsync(2, blockControl: true).Forget();
		});
		secureCutscene.SetInteractionActive(value: false);
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 0:
			base.PlayableDirector.playableAsset = openingTimeline;
			MoveCutsceneStartCamToCurrentCam(openingStartCam);
			MovePlayerAndLookUpAsync(openingStartPosition).Forget();
			LazySingleton<MessageSystem>.Instance.Subscribe<ImahenChaserUpdateEvent>(ImahenChaser);
			LazySingleton<MessageSystem>.Instance.Publish(new ImahenChaserUpdateEvent(ImahenChaser.Mode.Chase, chaserInitialPositions[0].position));
			break;
		case 2:
			base.PlayableDirector.playableAsset = endingTimeline;
			await UniTask.WaitUntil(() => CutsceneProducer.Player.mover.IsOnGround && CutsceneProducer.Player.mover.PSM.CurrentState is PlayerIdleState);
			await CutsceneProducer.Player.MoveXPositionAsync(endingStartPosition.position.x, isRunning: true);
			MoveCutsceneStartCamToCurrentCam(endingStartCam);
			CutsceneProducer.Player.mover.Flip(Direction.Right);
			BindPlayerTracks(value: true, tween: true);
			if (chaserObject.position.x >= failChaserPositionX)
			{
				base.PlayableDirector.TrackBinding<TweenTrack>("Chaser Tween Track", null);
				FailWithChaser(TaskUtil.RefreshToken(ref chaserFailTokenSource)).Forget();
			}
			else
			{
				base.PlayableDirector.TrackBinding<TweenTrack>("Chaser Tween Track", chaserObject);
			}
			break;
		case 4:
			base.PlayableDirector.playableAsset = endingFailedTimeline;
			LazySingleton<MessageSystem>.Instance.Publish(new ImahenChaserUpdateEvent(ImahenChaser.Mode.Chase));
			ChangeFigureToPlayer();
			break;
		case 6:
			base.PlayableDirector.playableAsset = endingSuccessTimeline;
			endingFinishCam.gameObject.SetActive(value: false);
			ImahenChaser.StopRumbling();
			break;
		case 1:
		case 3:
		case 5:
			break;
		}
	}

	protected override void OnEndTimeline(int state)
	{
		switch (state)
		{
		case 0:
			SynchronizeEndVcamAsync(openingFinishCam).Forget();
			CutsceneProducer.AddInteractCount(ActorName.Imahen);
			ImahenChaser.Rumbling().Forget();
			CutsceneProducer.SoundManager.PlayBGM("BossImahen");
			break;
		case 2:
			SustainVcamForFade(endingFinishCam);
			CutsceneProducer.InputManager.StackControlTarget(buttonAction).Forget();
			LazySingleton<MessageSystem>.Instance.Publish(new ImahenChaserUpdateEvent(ImahenChaser.Mode.Stop));
			break;
		case 6:
		{
			SynchronizeEndVcamAsync(endingSuccessFinishCam).Forget();
			ImahenChaser.Stop();
			transparentDoor.SetActive(value: true);
			cutsceneInteraction.gameObject.SetActive(value: false);
			secureCutscene.SetInteractionActive(value: true);
			CutsceneProducer.SoundManager.PlayBGM();
			ImahenChaserBreakable[] array = imahenChaserBreakables;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].ChangeState(1);
			}
			break;
		}
		}
	}

	protected override void OnSkipTimeline(int state)
	{
		base.PlayableDirector.time = base.PlayableDirector.duration;
		base.PlayableDirector.Evaluate();
		switch (state)
		{
		case 0:
			PlayAlert(value: true);
			SetChaserSfx(value: true);
			break;
		case 2:
			ChangePlayerToFigure();
			playerFigure.SynchronizePositionDirection(endingEndPosition);
			playerFigureSpine.SetAnimationInstantly("scene/imahen_lever_pull_ready");
			break;
		case 6:
			StopChaserParticles();
			PlayAlert(value: false);
			SetChaserSfx(value: false);
			ChangeFigureToPlayer();
			break;
		}
	}

	public override bool ShouldLoop(int index)
	{
		return CutsceneProducer.IsTalking;
	}

	public override void EnterLoop(int index)
	{
		StartScenarioInsideCutscene(index);
	}

	public override void ExitLoop(int index)
	{
	}

	public void PlayAlert(bool value)
	{
		CutsceneProducer.FieldCanvas.RedAlertBoard.SetAlert(value);
	}

	public void FlipPlayer(bool value)
	{
		CutsceneProducer.Player.mover.Flip(value ? Direction.Right : Direction.Left);
	}

	public void ChangePlayerToFigure()
	{
		playerFigure.SynchronizePositionDirection(CutsceneProducer.Player.transform);
		CutsceneProducer.Player.SetPosition(Util.DisablePosition);
		playerFigure.gameObject.SetActive(value: true);
		BindPlayerTracks(value: false, tween: true, PlayerFigureSpine, playerFigure);
	}

	public void ChangeFigureToPlayer()
	{
		CutsceneProducer.Player.SetPosition(playerFigure.position, playerFigure.localRotation.x >= 0f);
		playerFigure.gameObject.SetActive(value: false);
		CutsceneProducer.Player.mover.SynchronizeAnimation(playerFigureSpine);
		BindPlayerTracks(value: true, tween: true);
	}

	public void StopChaserParticles()
	{
		chaserParticles.ForEach(delegate(ParticleSystem p)
		{
			p.Stop();
		});
	}

	public void SetChaserSfx(bool value)
	{
		if (value)
		{
			ImahenChaser.PlaySfx();
		}
		else
		{
			ImahenChaser.StopSfx();
		}
	}

	public void StopChaserSfx()
	{
		ImahenChaser.StopSfx();
	}

	private async UniTask ActivateRetry()
	{
		CutsceneProducer.SoundManager.PlayBGM("BossImahen");
		chaserObject.position = chaserStartPositionRetry.position;
		chaserObject.gameObject.SetActive(value: true);
		SetChaserSfx(value: true);
		PlayAlert(value: true);
		PlayChaserParticles();
		if (CutsceneProducer.EnemyFightData.IsBossCleared(BossName.Imahen))
		{
			CutsceneProducer.SoundManager.PlayBGM(bossArea.SoundKey);
		}
		await chaserObject.DOMove(chaserEndPositionRetry.position, 1.3333334f).SetEase(Ease.InOutSine).WaitAsync();
		LazySingleton<MessageSystem>.Instance.Subscribe<ImahenChaserUpdateEvent>(ImahenChaser);
		LazySingleton<MessageSystem>.Instance.Publish(new ImahenChaserUpdateEvent(ImahenChaser.Mode.Chase));
		ImahenChaser.Rumbling().Forget();
	}

	private void PlayChaserParticles()
	{
		chaserParticles.ForEach(delegate(ParticleSystem p)
		{
			p.gameObject.SetActive(value: true);
			p.Play();
		});
	}

	private async UniTask MovePlayerAndLookUpAsync(Transform target)
	{
		await CutsceneProducer.Player.MoveXPositionAsync(target.position.x, isRunning: true);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(5);
		PlayerLookUp();
	}

	private async UniTask FailWithChaser(CancellationToken token)
	{
		float targetSpeed = (failedChaserFinishPosition.position.x - failedChaserStartPosition.position.x) / chaserMoveDuration;
		float timer = 0f;
		float speed = 0f;
		if (chaserObject.position.x <= failedChaserStartPosition.position.x)
		{
			DebugLog.GeneralLog($"chaser : {chaserObject.position.x}, point : {failedChaserStartPosition.position.x}");
			chaserObject.position = failedChaserStartPosition.position;
		}
		else
		{
			DebugLog.GeneralLog("Do Nothing!");
		}
		while (timer < chaserMoveDuration && !chaserObject.IsNull())
		{
			chaserObject.Translate(speed * Time.deltaTime, 0f, 0f);
			if (speed <= targetSpeed)
			{
				speed += 10f * Time.deltaTime;
			}
			timer += Time.deltaTime;
			await UniTask.Yield(token);
		}
	}

	public async UniTask ResetCutscene(int state)
	{
		if (CutsceneProducer.Player.isDead)
		{
			DebugLog.GeneralLogWarning("플레이어가 죽었으므로 컷씬을 따로 초기화하지 않습니다!");
			return;
		}
		IsResetting = true;
		NullControllable nullControllable = new NullControllable("Reset Cutscene");
		CutsceneProducer.InputManager.StackControlTarget(nullControllable).Forget();
		await UniTask.WaitForSeconds(0.5f);
		await CutsceneProducer.FadeCanvas.FadeOut(1f);
		base.PlayableDirector.Stop();
		StopChaserParticles();
		await UniTask.WaitForSeconds(1);
		CutsceneProducer.Player.SetPosition(resetPlayerPosition.position);
		CutsceneProducer.ExecuteWithoutChangeBGM(delegate
		{
			base.FieldController.ChangeAreaTo(base.FieldController.MainArea);
		});
		ImahenChaser.transform.SynchronizePositionDirection(resetChaserPosition);
		PlayChaserParticles();
		chaserFailTokenSource?.Cancel();
		leverSpine.SetAnimation("static");
		leverWhite.SetAlpha(1f);
		if (state == 4)
		{
			SustainVcamForFade(endingFinishCam);
			endingFinishCam.gameObject.SetActive(value: false);
			chasaFigure.GetComponentInChildren<SkeletonAnimation>().SetAnimationInstantly("scene/imahen/lever_landing");
			chasaFigure.gameObject.SetActive(value: false);
			ImahenChaser.Rumbling().Forget();
		}
		await CutsceneProducer.FadeCanvas.FadeIn(1f);
		await UniTaskExtension.WaitForSeconds(0.5f);
		LazySingleton<MessageSystem>.Instance.Publish(new ImahenChaserUpdateEvent(ImahenChaser.Mode.BossEnd, resetChaserPosition.position));
		await CutsceneProducer.InputManager.RemoveControlTarget(nullControllable);
		BreakTimeline();
		IsResetting = false;
	}
}
