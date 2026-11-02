using System;
using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using Spine.Unity;
using Spine.Unity.Playables;
using Timeline.Samples;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using UnityEngine.Timeline;

namespace SPR.Cutscene;

public class VeritaCutsceneProducer : BossCutsceneProducer
{
	[Serializable]
	private class LightControlInfo
	{
		[SerializeField]
		private Light2D source;

		[SerializeField]
		private float interval;

		private float fullIntensity = -1f;

		private static int levelCount = -1;

		public static void SetLevelCount(int value)
		{
			if (levelCount < 0)
			{
				levelCount = value;
			}
		}

		public void SetIntensity(float currentNum)
		{
			if (fullIntensity < 0f)
			{
				fullIntensity = source.intensity;
			}
			source.intensity = fullIntensity - interval * ((float)levelCount - currentNum);
		}
	}

	[Header("Timeline Assets")]
	[SerializeField]
	private PlayableAsset openingTimeline;

	[SerializeField]
	private PlayableAsset phaseTwoTimeline;

	[SerializeField]
	private PlayableAsset phaseThreeTimeline;

	[SerializeField]
	private PlayableAsset endingTimeline;

	[FormerlySerializedAs("teleportTimeline")]
	[SerializeField]
	private PlayableAsset truthTimeline;

	[SerializeField]
	private PlayableAsset videoTimeline;

	[Header("Opening")]
	[SerializeField]
	private CinemachineVirtualCamera openingStartCam;

	[SerializeField]
	private CinemachineVirtualCamera openingFinishCam;

	[Header("Phase Two Transition")]
	[SerializeField]
	private Transform phaseTwoCenterPosition;

	[SerializeField]
	private Transform phaseTwoDownKickVfx;

	[SerializeField]
	private Zone phaseTwoDownKickZone;

	[SerializeField]
	private float phaseTwoExplosionRadius;

	[SerializeField]
	private string phaseTwoExplosionAttackKey;

	[SerializeField]
	private int forceDurationFrames;

	[SerializeField]
	private float forceMagnitude;

	[Header("Phase Three Transition")]
	[SerializeField]
	private CinemachineVirtualCamera phaseThreeCam;

	[SerializeField]
	private CinemachineVirtualCamera phaseThreeMoveCam;

	[SerializeField]
	private List<Transform> phaseThreePositions;

	[SerializeField]
	private Vector2 knockBackVelocity;

	[SerializeField]
	private VeritaAura veritaAura;

	[Header("Ending")]
	[SerializeField]
	private CinemachineVirtualCamera endingCam;

	[SerializeField]
	private Transform endingVeritaPosition;

	[SerializeField]
	private Transform endingPlayerPosition;

	[SerializeField]
	private List<Transform> endingPositions;

	[SerializeField]
	private CinemachineVirtualCamera videoCam;

	[Header("Truth")]
	[SerializeField]
	private CinemachineVirtualCamera teleportCam;

	[SerializeField]
	private CinemachineVirtualCamera truthEnd;

	[SerializeField]
	private float yourMajestyFadeOutDuration;

	[SerializeField]
	private float yourMajestyFadeInDuration;

	[SerializeField]
	private float yourMajestyTextFadeDuration;

	[SerializeField]
	private float yourMajestyTextDuration;

	[Header("References")]
	[SerializeField]
	private CutsceneTrigger cutsceneTrigger;

	[SerializeField]
	private VeritaDarkBackgroundController darkBackground;

	[SerializeField]
	private List<GameObject> transparentWalls;

	[SerializeField]
	private List<TorchController> torches;

	[SerializeField]
	private List<LightControlInfo> lights;

	[SerializeField]
	private GameObject veritaDeskObject;

	[Header("Values")]
	[SerializeField]
	private float camXMin;

	[SerializeField]
	private float camXMax;

	[SerializeField]
	private float lightsInterval;

	[SerializeField]
	private AnimationCurve brighteningCurve;

	[SerializeField]
	private int torchStartDelay;

	[SerializeField]
	private int torchLightDelay;

	[Header("Video")]
	[SerializeField]
	private int[] narrationStartTimings;

	private Verita veritaBoss;

	private NpcVerita veritaNpc;

	private bool isPhaseIncreased;

	private int currentTorchNum;

	private double pauseTime;

	private UniTask playerMoveEndingTask;

	private bool shiftNarration;

	private CancellationToken cancellationToken;

	private GameObject cutsceneCanvas;

	private Verita VeritaBoss => veritaBoss ?? (veritaBoss = Boss.CastObject<Verita>());

	private NpcVerita VeritaNpc => veritaNpc ?? (veritaNpc = base.FightArea.transform.parent.GetComponentInChildren<NpcVerita>(includeInactive: true));

	protected override Vector2 KnockBackVelocity => knockBackVelocity;

	public static bool IsVeritaInfected
	{
		get
		{
			if (Trigger.CheckTrigger("Trigger_VeritaInfected"))
			{
				return !CutsceneProducer.EnemyFightData.IsBossCleared(BossName.Verita);
			}
			return false;
		}
	}

	protected override void SetProducerState(int state)
	{
		switch (state)
		{
		case 0:
		case 1:
		case 2:
		case 4:
		case 6:
		{
			bool flag = !IsVeritaInfected;
			ShowNpc(flag);
			if (!flag)
			{
				LightControlInfo.SetLevelCount(torches.Count);
				lights.ForEach(delegate(LightControlInfo lci)
				{
					lci.SetIntensity(0f);
				});
			}
			break;
		}
		case 9:
			ShowNpc(value: true);
			break;
		case 3:
		case 5:
		case 7:
		case 8:
			break;
		}
		void ShowNpc(bool value)
		{
			VeritaNpc.gameObject.SetActive(value);
			veritaDeskObject?.SetActive(value);
			if (!value)
			{
				cutsceneTrigger.OnTriggered += delegate
				{
					PlayTimelineAsync(1, blockControl: true).Forget();
				};
			}
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		cancellationToken = token;
		switch (state)
		{
		case 1:
			base.PlayableDirector.playableAsset = openingTimeline;
			await BlendFieldCamToCutsceneStartCamAsync(openingStartCam);
			bossFigure.gameObject.SetActive(value: true);
			break;
		case 3:
			base.PlayableDirector.playableAsset = phaseTwoTimeline;
			isPhaseIncreased = false;
			Boss.IsImmortal = false;
			Boss.gameObject.SetActive(value: true);
			base.PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Spine Animation State Track", Boss.SkeletonAnimation);
			base.PlayableDirector.TrackBinding<AnimationTrack>("Animation Track", Boss.SkeletonAnimation.GetComponent<Animator>());
			base.PlayableDirector.TrackBinding<TweenTrack>("Tween Track", Boss.transform);
			break;
		case 5:
			base.PlayableDirector.playableAsset = phaseThreeTimeline;
			isPhaseIncreased = false;
			AdjustPositionsPhaseThree();
			SwitchToBossFigure();
			await BlendFieldCamToCutsceneStartCamAsync(phaseThreeCam);
			break;
		case 7:
			base.PlayableDirector.playableAsset = endingTimeline;
			ClearFight(updateMemory: true, stopBgm: true, getHaechiReward: false);
			veritaAura.StartEnding(base.BossSpine);
			FlipBossToPlayer();
			AdjustPositionsEnding();
			SwitchToBossFigure();
			MoveCutsceneStartCamToCurrentCam(endingCam);
			break;
		case 8:
			base.PlayableDirector.playableAsset = truthTimeline;
			base.PlayableDirector.TrackBinding<ActivationTrack>("Player Activation Track", CutsceneProducer.Player.transform.gameObject);
			base.PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Player Spine Animation State Track", CutsceneProducer.Player.mover.SkeletonAnimation);
			teleportCam.gameObject.SetActive(value: false);
			AddressableHelper.PreloadAssetAsync("Verita Cutscene Canvas.prefab").Forget();
			break;
		case 2:
		case 4:
		case 6:
			break;
		}
	}

	protected override void OnEndTimeline(int state)
	{
		switch (state)
		{
		case 1:
			SwitchToRealBoss();
			SynchronizeEndVcamAsync(openingFinishCam).Forget();
			transparentWalls.ForEach(delegate(GameObject x)
			{
				x.SetActive(value: true);
			});
			StartFight();
			break;
		case 3:
			ExitBossCutsceneState();
			isPhaseIncreased = false;
			break;
		case 5:
			SwitchToRealBoss();
			SynchronizeEndVcamAsync(phaseThreeCam).Forget();
			isPhaseIncreased = false;
			veritaAura.Initialize(Boss);
			break;
		case 7:
		{
			base.BossSpine.SynchronizeAnimation(base.BossSpine);
			transparentWalls.ForEach(delegate(GameObject x)
			{
				x.SetActive(value: false);
			});
			UniTask task = PlayTeleportTimelineAsync();
			CutsceneProducer.InputManager.BlockControlForTask(task, "Verita Timeline Ending to Teleport").Forget();
			EarnMoney();
			break;
		}
		case 8:
			AddressableHelper.ReleaseInstance(cutsceneCanvas);
			AddressableHelper.UnloadAsset("Verita Cutscene Canvas.prefab");
			cutsceneCanvas = null;
			Game.Instance.LoadFieldAsync(SceneDataModel.MoveCutsceneModel(new FieldID(SectorName.Library, 4), "VeritaGate")).Forget();
			break;
		case 2:
		case 4:
		case 6:
			break;
		}
	}

	protected override void OnSkipTimeline(int state)
	{
		OnBeforeSkipTimeline(state != 7);
		switch (state)
		{
		case 1:
			ChangeArea();
			darkBackground.SetActive(isActive: true, instant: true);
			SetActiveTorches(value: true);
			lights.ForEach(delegate(LightControlInfo lci)
			{
				lci.SetIntensity(torches.Count);
			});
			break;
		case 5:
			if (!isPhaseIncreased)
			{
				IncreasePhase();
			}
			break;
		case 7:
			StopAura();
			DisableFadeInAfterSkip();
			break;
		case 8:
			DisableFadeInAfterSkip();
			break;
		}
	}

	private async UniTask PlayTeleportTimelineAsync()
	{
		await CutsceneProducer.FadeCanvas.FadeOut(3f);
		await UniTask.WaitForSeconds(2);
		CutsceneProducer.Player.transform.SynchronizePositionDirection(endingPlayerPosition);
		SustainVcamForFade(teleportCam);
		await CutsceneProducer.FadeCanvas.FadeIn(3f);
		PlayTimelineAsync(8, blockControl: true).Forget();
	}

	public override bool ShouldLoop(int index)
	{
		return base.CurrentState switch
		{
			8 => (index == 100) ? (!shiftNarration) : CutsceneProducer.IsTalking, 
			7 => playerMoveEndingTask.Status != UniTaskStatus.Succeeded, 
			_ => false, 
		};
	}

	public override void EnterLoop(int index)
	{
		switch (base.CurrentState)
		{
		case 8:
			if (index == 100)
			{
				CutsceneProducer.FieldCanvas.NarrationBoard.EnableControlGuide(enable: true);
				shiftNarration = false;
			}
			else
			{
				StartScenarioInsideCutscene(index);
			}
			break;
		case 7:
			if (index == 0)
			{
				CutsceneProducer.InputManager.StackControlTarget(this).Forget();
				playerMoveEndingTask = MovePlayerAtEndingAsync();
			}
			break;
		}
	}

	public override void ExitLoop(int index)
	{
		switch (base.CurrentState)
		{
		case 7:
			if (index == 0)
			{
				BindPlayerTracks(value: true, tween: true);
			}
			break;
		case 8:
			if (index == 100)
			{
				shiftNarration = false;
				CutsceneProducer.FieldCanvas.NarrationBoard.EnableControlGuide(enable: false);
			}
			break;
		}
	}

	public void IncreasePhase()
	{
		if (bossFigure.gameObject.activeSelf)
		{
			Boss.transform.SynchronizePositionDirection(bossFigure);
		}
		Boss.IncreasePhase();
		isPhaseIncreased = true;
	}

	public void FadeOut()
	{
		CutsceneProducer.UIManager.FadeCanvas.FadeOut(0.5f).Forget();
	}

	public void SetDarkBackground(bool value)
	{
		darkBackground.transform.position = Boss.VisualBottom;
		darkBackground.SetActive(value);
		if (value)
		{
			TurnOnTorchesAsync().Forget();
		}
	}

	public void StartQuake()
	{
		ShakeCamera(3f, 2f);
	}

	public void EndQuake()
	{
		ShakeCamera();
	}

	public void DownKickOnTimeline()
	{
		Boss.AttackSquare(phaseTwoDownKickZone, new HitInfo(Boss, "Down_Kick"), new List<IDamageable>());
	}

	public void ExplosionOnTimeline()
	{
		ApplyStompForceAgainstBoss(forceDurationFrames, forceMagnitude);
	}

	public void SetActiveTorches(bool value)
	{
		torches.ForEach(delegate(TorchController t)
		{
			t.SetActive(value);
		});
	}

	public void DeactivateSpace()
	{
		VeritaBoss.SetBackground(shouldBlack: false);
	}

	public void HideVeritaAtPhaseTwo()
	{
		Boss.IsImmortal = true;
		Boss.transform.position = phaseTwoCenterPosition.position;
		FlipBossToPlayer();
		phaseTwoDownKickVfx.localScale = Boss.transform.localScale;
	}

	public void ShowVeritaAtPhaseTwo()
	{
		Boss.IsImmortal = false;
	}

	public void SwitchVideoTimeline(bool toVideo)
	{
		if (toVideo)
		{
			SwitchVideoTimelineAsync().Forget();
		}
		else
		{
			SwitchOriginTimelineAsync().Forget();
		}
	}

	public void ChangeSkin(string skinName)
	{
		base.BossSpine.Skeleton.SetSkin(skinName);
	}

	public void FadeOutNarration(string id)
	{
		ShowFadeOutNarration(id).Forget();
	}

	public void StopAura()
	{
		DebugLog.GeneralLog("StopAura");
		veritaAura.FinalizeUpdate();
	}

	public override void Control()
	{
		base.Control();
		if (!shiftNarration)
		{
			shiftNarration = base.PlayableDirector.playableAsset == videoTimeline && CutsceneProducer.InputManager.confirm;
		}
	}

	private void AdjustPositionsPhaseThree()
	{
		float diffX = Boss.transform.position.x - phaseThreeCam.transform.position.x;
		BossCutsceneProducer.AlignTransformXs(phaseThreePositions, diffX, flipCondition: false);
		Vector3 position = phaseThreeMoveCam.transform.position;
		phaseThreeMoveCam.transform.position = new Vector3(base.FightArea.GetClampCameraX(phaseThreeMoveCam), position.y, position.z);
	}

	private void AdjustPositionsEnding()
	{
		FlipBossToTarget(base.FightArea.AreaBox.Center.x);
		float diffX = Boss.transform.position.x - endingVeritaPosition.position.x;
		BossCutsceneProducer.AlignTransformXs(endingPositions, diffX, Boss.transform.localScale.x > 0f, Boss.transform.position.x);
	}

	private async UniTask MovePlayerAtEndingAsync()
	{
		await CutsceneProducer.Player.MoveXPositionAsync(endingPlayerPosition.position.x, isRunning: true);
		CutsceneProducer.Player.mover.Flip((!(CutsceneProducer.Player.transform.position.x > bossFigure.position.x)) ? Direction.Right : Direction.Left);
	}

	private async UniTask TurnOnTorchesAsync()
	{
		float timer = 0f;
		LightControlInfo.SetLevelCount(torches.Count);
		for (currentTorchNum = -torchStartDelay; currentTorchNum <= torches.Count + 1 + torchLightDelay; currentTorchNum++)
		{
			if (currentTorchNum > 0 && currentTorchNum <= torches.Count)
			{
				torches[currentTorchNum - 1].SetActive(value: true);
			}
			while (timer < lightsInterval)
			{
				float num = Mathf.Max(0f, (float)(currentTorchNum - 2 - torchLightDelay) + brighteningCurve.Evaluate(timer / lightsInterval));
				lights.ForEach(delegate(LightControlInfo lci)
				{
					lci.SetIntensity(num);
				});
				timer += Time.deltaTime;
				await UniTask.Yield();
			}
			timer -= lightsInterval;
		}
		lights.ForEach(delegate(LightControlInfo lci)
		{
			lci.SetIntensity(4f);
		});
	}

	private async UniTaskVoid SwitchVideoTimelineAsync()
	{
		pauseTime = Util.FrameToSeconds(1650f);
		base.PlayableDirector.Pause();
		await CutsceneProducer.FadeCanvas.FadeWhiteOut(0.1f, 0f, cancellationToken);
		await UniTask.WaitForSeconds(3f, ignoreTimeScale: false, PlayerLoopTiming.Update, cancellationToken);
		cutsceneCanvas = (await AddressableHelper.InstantiateAsync<Transform>("Verita Cutscene Canvas.prefab", base.transform)).gameObject;
		base.PlayableDirector.playableAsset = videoTimeline;
		SkeletonGraphic[] componentsInChildren = cutsceneCanvas.GetComponentsInChildren<SkeletonGraphic>(includeInactive: true);
		base.PlayableDirector.TrackBinding<SpineAnimationStateGraphicTrack>("Cutscene1", componentsInChildren[0]);
		base.PlayableDirector.TrackBinding<SpineAnimationStateGraphicTrack>("Cutscene2", componentsInChildren[1]);
		base.PlayableDirector.TrackBinding<SpineAnimationStateGraphicTrack>("Cutscene3", componentsInChildren[2]);
		base.PlayableDirector.TrackBinding<SpineAnimationStateGraphicTrack>("Cutscene4", componentsInChildren[3]);
		base.PlayableDirector.TrackBinding<ActivationTrack>("Cutscene1 Activation", componentsInChildren[0].gameObject);
		base.PlayableDirector.TrackBinding<ActivationTrack>("Cutscene2 Activation", componentsInChildren[1].gameObject);
		base.PlayableDirector.TrackBinding<ActivationTrack>("Cutscene3 Activation", componentsInChildren[2].gameObject);
		base.PlayableDirector.TrackBinding<ActivationTrack>("Cutscene4 Activation", componentsInChildren[3].gameObject);
		base.PlayableDirector.time = 0.0;
		base.PlayableDirector.Evaluate();
		CutsceneProducer.SoundManager.PlayBGM("VeritaTruth");
		await CutsceneProducer.FadeCanvas.FadeIn(2f, 0f, Ease.Linear, cancellationToken);
		base.PlayableDirector.Play();
	}

	private async UniTaskVoid SwitchOriginTimelineAsync()
	{
		base.PlayableDirector.Pause();
		await CutsceneProducer.FadeCanvas.FadeOut(2f);
		base.PlayableDirector.playableAsset = truthTimeline;
		base.PlayableDirector.time = pauseTime;
		base.PlayableDirector.Evaluate();
		CutsceneProducer.SoundManager.StopBGM();
		await CutsceneProducer.FadeCanvas.FadeIn(1f);
		base.PlayableDirector.Play();
	}

	private async UniTaskVoid ShowFadeOutNarration(string id)
	{
		await CutsceneProducer.FadeCanvas.FadeOut(yourMajestyFadeOutDuration, 0f, cancellationToken);
		await CutsceneProducer.FadeCanvas.ShowNarrationAsync(id, yourMajestyTextFadeDuration, yourMajestyTextDuration, cancellationToken);
		await CutsceneProducer.FadeCanvas.FadeIn(yourMajestyFadeInDuration, 0f, Ease.Linear, cancellationToken);
	}
}
