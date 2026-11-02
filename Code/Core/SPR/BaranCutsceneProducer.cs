using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using Library;
using SPR.Cutscene;
using Spine.Unity;
using Spine.Unity.Playables;
using Timeline.Samples;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public class BaranCutsceneProducer : BossCutsceneProducer
{
	[Header("Timelines")]
	[SerializeField]
	private PlayableAsset openingTimeline;

	[SerializeField]
	private PlayableAsset phaseTwoTimeline;

	[SerializeField]
	private PlayableAsset phaseThreeTimeline;

	[SerializeField]
	private PlayableAsset endingTimeline;

	[SerializeField]
	private PlayableAsset dyingTimeline;

	[SerializeField]
	private PlayableAsset dyingMyosoTimeline;

	[Header("Opening")]
	[SerializeField]
	private CinemachineVirtualCamera openingCam;

	[Header("Phase Two Transition")]
	[SerializeField]
	private Transform phaseTwoBaranPosition;

	[SerializeField]
	private List<Transform> phaseTwoPositions;

	[SerializeField]
	private BoneFollower tearFollower;

	[Space]
	[SerializeField]
	private int impulseDurationFrames;

	[SerializeField]
	private float impulseMagnitude;

	[SerializeField]
	private int windForceDurationFrames;

	[SerializeField]
	private float windForceMagnitude;

	[Header("Phase Three Transition")]
	[SerializeField]
	private CinemachineVirtualCamera phaseThreeStartCam;

	[SerializeField]
	private CinemachineVirtualCamera phaseThreeZoomCam;

	[SerializeField]
	private CinemachineVirtualCamera phaseThreeFinishCam;

	[SerializeField]
	private Transform phaseThreeBaranPosition;

	[SerializeField]
	private List<Transform> phaseThreePositions;

	[SerializeField]
	private Vector2 knockBackVelocity;

	[SerializeField]
	private float phaseThreeCamXMin;

	[SerializeField]
	private float phaseThreeCamXMax;

	[Header("Ending")]
	[SerializeField]
	private CinemachineVirtualCamera endingCam;

	[SerializeField]
	private Transform endingBaranPosition;

	[SerializeField]
	private Transform endingPlayerPosition;

	[SerializeField]
	private List<Transform> endingPositions;

	[SerializeField]
	private List<Transform> endingPositionsWithoutFlip;

	[SerializeField]
	private List<Transform> endingVcamPositions;

	[SerializeField]
	private float endingCamXMin;

	[SerializeField]
	private float endingCamXMax;

	[Header("Dying")]
	[SerializeField]
	private CinemachineVirtualCamera dyingCam;

	[Header("Dying Myoso")]
	[SerializeField]
	private CinemachineVirtualCamera dyingMyosoStartCam;

	[SerializeField]
	private CinemachineVirtualCamera dyingMyosoEndCam;

	[SerializeField]
	private Transform dyingMyosoMyosoPosition;

	[SerializeField]
	private Transform dyingMyosoPlayerPosition;

	[SerializeField]
	private ParticleSystem dyingMyosoParticle;

	[SerializeField]
	private float dyingMyosoCameraMargin;

	[Header("References")]
	[SerializeField]
	private Transform myosoFigure;

	[SerializeField]
	private CutsceneTrigger cutsceneTrigger;

	[SerializeField]
	private List<GameObject> transparentWalls;

	[SerializeField]
	private GameObject entranceWall;

	[SerializeField]
	private ParticleSystem rainVfx;

	[SerializeField]
	private GameObject rainSfxEmitter;

	[SerializeField]
	private EventReference myosoStartEvent;

	[SerializeField]
	private EventReference myosoBgmEvent;

	[SerializeField]
	private EventReference myosoTalkBgmEvent;

	[Header("Values")]
	[SerializeField]
	private float rainStopDelay;

	private SkeletonAnimation myosoSpine;

	private EventInstance myosoBgmInstance;

	protected override List<int> UnskippableCutscenes => new List<int> { 6 };

	protected override Vector2 KnockBackVelocity => knockBackVelocity;

	protected override string ResonanceID => base.ResonanceID + (WithMyoso ? "_WithMyoso" : null);

	private SkeletonAnimation MyosoSpine => myosoSpine ?? (myosoSpine = myosoFigure.GetComponentInChildren<SkeletonAnimation>(includeInactive: true));

	private static bool WithMyoso => Trigger.CheckTrigger("Trigger_MyosoHelpFightBaran");

	protected override void SetProducerState(int state)
	{
		entranceWall.SetActive(value: false);
		switch (state)
		{
		case 0:
		case 1:
		case 3:
		case 5:
			cutsceneTrigger.OnTriggered += delegate
			{
				PlayTimelineAsync(0, blockControl: true).Forget();
			};
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: false);
			});
			break;
		case 8:
		case 10:
			ActivateCorpse();
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: false);
			});
			break;
		case 2:
		case 4:
		case 6:
		case 7:
		case 9:
			break;
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 0:
			base.PlayableDirector.playableAsset = openingTimeline;
			await BlendFieldCamToCutsceneStartCamAsync(openingCam);
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: true);
			});
			break;
		case 2:
			base.PlayableDirector.playableAsset = phaseTwoTimeline;
			AdjustPositionsPhaseTwo();
			Boss.IsImmortal = false;
			Boss.gameObject.SetActive(value: true);
			base.PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Spine Animation State Track", Boss.SkeletonAnimation);
			base.PlayableDirector.TrackBinding<TweenTrack>("Tween Track", Boss.transform);
			break;
		case 4:
			base.PlayableDirector.playableAsset = phaseThreeTimeline;
			AdjustPositionsPhaseThree();
			SwitchToBossFigure();
			await BlendFieldCamToCutsceneStartCamAsync(phaseThreeStartCam);
			break;
		case 6:
			base.PlayableDirector.playableAsset = endingTimeline;
			ClearFight();
			FlipBossToPlayer();
			AdjustPositionsEnding();
			SwitchToBossFigure();
			MoveCutsceneStartCamToCurrentCam(endingCam);
			CutsceneProducer.Player.StatusEffectController.SetEffectLoop(active: false);
			break;
		case 7:
			base.PlayableDirector.playableAsset = dyingTimeline;
			break;
		case 9:
			base.PlayableDirector.playableAsset = dyingMyosoTimeline;
			base.BossSpine.SetAnimation("acc_spin_slow", 1);
			break;
		}
		await UniTask.CompletedTask;
	}

	protected override void OnEndTimeline(int state)
	{
		switch (state)
		{
		case 0:
			SwitchToRealBoss();
			SynchronizeEndVcamAsync(openingCam).Forget();
			StartFight();
			break;
		case 2:
			ExitBossCutsceneState();
			break;
		case 4:
			SwitchToRealBoss();
			SynchronizeEndVcamAsync(phaseThreeFinishCam).Forget();
			break;
		case 6:
		{
			base.BossSpine.SynchronizeAnimation(base.BossSpine);
			UniTask task = PlayDyingTimelineAsync();
			CutsceneProducer.InputManager.BlockControlForTask(task, "Baran Timeline : Ending to Dying").Forget();
			break;
		}
		case 7:
			SynchronizeEndVcamAsync(dyingCam).Forget();
			EndDyingTimeline().Forget();
			break;
		case 9:
			dyingMyosoStartCam.gameObject.SetActive(value: false);
			BindPlayerTracks(value: false);
			SynchronizeEndVcamAsync(dyingMyosoEndCam).Forget();
			EndDyingTimeline().Forget();
			CutsceneProducer.MemoryPanel.TryUpdateClearedMemory(wasCleared: false, ActorName.Myoso);
			break;
		case 1:
		case 3:
		case 5:
		case 8:
			break;
		}
	}

	private async UniTaskVoid EndDyingTimeline()
	{
		bossFigure.gameObject.SetActive(value: false);
		await UniTask.WaitForSeconds(rainStopDelay);
		StopRain();
		transparentWalls.ForEach(delegate(GameObject wall)
		{
			wall.SetActive(value: false);
		});
		CutsceneProducer.Field.ChangeAreaTo(OriginArea);
		entranceWall.SetActive(value: false);
		CutsceneProducer.Player.StatusEffectController.SetEffectLoop(active: true);
		new PureItem("BaranSpirit").AddToPlayerData(ItemAcquireDisplayOrder.ShowFullPanel);
		await UniTask.WaitWhile(() => CutsceneProducer.FieldCanvas.InteractionPanel.IsDisplaying);
		CutsceneProducer.Player.PlayerStat.AddStatAppliedExperience(Boss.Exp);
		Game.Instance.Player.PlayerPart.OnKill();
		EarnMoney();
		await UniTask.WaitWhile(() => CutsceneProducer.FieldCanvas.LevelBoard.IsLevelUpProducing);
		new PureItem("RoyalSealPremium").AddToPlayerData(ItemAcquireDisplayOrder.ShowNotification);
		ShowResonance();
	}

	protected override void OnSkipTimeline(int state)
	{
		OnBeforeSkipTimeline();
		switch (state)
		{
		case 0:
			ChangeArea();
			break;
		case 9:
			SetDyingMyosoOnFade();
			if (myosoBgmInstance.isValid())
			{
				StopMyosoBgm();
			}
			break;
		}
	}

	public override bool TryCutsceneOnPlayerDie()
	{
		if (Trigger.CheckTrigger("Trigger_BaranShouldGuideMyoso"))
		{
			CutsceneProducer.WanderingData.EnqueueResonanceAfterMoveScene("BaranGuideMyoso_Resonance");
		}
		return base.TryCutsceneOnPlayerDie();
	}

	private async UniTask PlayDyingTimelineAsync()
	{
		await UniTask.WaitWhile(() => IsProducing);
		if (WithMyoso)
		{
			CutsceneProducer.Player.SetPosition(Util.DisablePosition);
			base.BossSpine.SynchronizeAnimation(base.BossSpine);
			dyingMyosoStartCam.gameObject.SetActive(value: true);
			myosoBgmInstance = CutsceneProducer.SoundManager.PlayOneShot(myosoBgmEvent);
			await UniTask.WaitForSeconds(2);
			CutsceneProducer.SoundManager.PlayOneShot(myosoStartEvent);
			await CutsceneProducer.FadeCanvas.FadeIn(3f, 0f, Ease.InQuad);
			await UniTask.WaitForSeconds(1);
			PlayTimelineAsync(9, blockControl: true).Forget();
		}
		else
		{
			PlayTimelineAsync(7).Forget();
		}
	}

	public override bool ShouldLoop(int index)
	{
		if (base.CurrentState == 9)
		{
			return CutsceneProducer.IsTalking;
		}
		return false;
	}

	public override void EnterLoop(int index)
	{
		if (base.CurrentState == 9)
		{
			StartScenarioInsideCutscene(index);
		}
	}

	public void StompForce()
	{
		ApplyStompForceAgainstBoss(impulseDurationFrames, impulseMagnitude);
		UniTaskExtension.WaitForFixedUpdatesOrCancel(impulseDurationFrames).GetAwaiter().OnCompleted(delegate
		{
			ApplyStompForceAgainstBoss(windForceDurationFrames, windForceMagnitude);
		});
	}

	public void FlipBossToRightOrPlayer(bool value)
	{
		Vector3 localScale = bossFigure.localScale;
		int num = (value ? 1 : ((bossFigure.position.x < CutsceneProducer.Player.mover.VisualBottom.x) ? 1 : (-1)));
		bossFigure.localScale = new Vector3((float)num * Mathf.Abs(localScale.x), localScale.y, localScale.z);
	}

	public void StartDyingMyoso()
	{
		if (WithMyoso)
		{
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: false);
			});
			entranceWall.SetActive(value: true);
			dyingMyosoParticle?.gameObject.SetActive(value: true);
			dyingMyosoParticle?.Play();
			CutsceneProducer.InputManager.StackControlTarget(this).Forget();
			CutsceneProducer.FadeCanvas.FadeOut(1f).Forget();
			CutsceneProducer.Player.mover.SetAnimation("guard_break", loop: false);
			CutsceneProducer.Player.mover.AddAnimation("idle");
		}
	}

	public void SetDyingMyosoOnFade()
	{
		StopRain();
		BindPlayerTracks(value: true);
		CutsceneProducer.Player.SetPosition(dyingMyosoPlayerPosition.position, dyingMyosoPlayerPosition.localScale.x > 0f);
		myosoFigure.Flip(dyingMyosoMyosoPosition.localScale.x);
		MyosoSpine.SetAnimation("acc_spin_slow", 1);
	}

	public void SetMyosoBgmParameter(int value)
	{
		myosoBgmInstance.setParameterByName("Local_Index", value);
	}

	public void ChangeMyosoBgm()
	{
		myosoBgmInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
		myosoBgmInstance.release();
		myosoBgmInstance = CutsceneProducer.SoundManager.PlayOneShot(myosoTalkBgmEvent);
	}

	public void StopMyosoBgm()
	{
		myosoBgmInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
		myosoBgmInstance.release();
	}

	private void AdjustPositionsPhaseTwo()
	{
		FlipBossToPlayer();
		float diffX = Boss.transform.position.x - phaseTwoBaranPosition.position.x;
		BossCutsceneProducer.AlignTransformXs(phaseTwoPositions, diffX, Boss.transform.localScale.x < 0f, Boss.transform.position.x);
		tearFollower.skeletonRenderer = Boss.SkeletonAnimation;
		Vector3 localScale = tearFollower.transform.localScale;
		float num = Mathf.Sign(Boss.transform.localScale.x);
		tearFollower.transform.localScale = new Vector3(num * Mathf.Abs(localScale.x), localScale.y, localScale.z);
	}

	private void AdjustPositionsPhaseThree()
	{
		float diffX = Boss.transform.position.x - phaseThreeBaranPosition.position.x;
		BossCutsceneProducer.AlignTransformXs(phaseThreePositions, diffX, flipCondition: false);
		Vector3 position = phaseThreeZoomCam.transform.position;
		phaseThreeZoomCam.transform.position = new Vector3(Mathf.Clamp(position.x, phaseThreeCamXMin, phaseThreeCamXMax), position.y, position.z);
	}

	private void AdjustPositionsEnding()
	{
		FlipBossToPlayer();
		float x = Boss.transform.position.x;
		bool flipCondition = CutsceneProducer.Player.mover.VisualBottom.x > x;
		float diffX = Boss.transform.position.x - endingBaranPosition.position.x;
		float num = (WithMyoso ? dyingMyosoCameraMargin : 0f);
		float camDiffX = Mathf.Clamp(Boss.transform.position.x, endingCamXMin - num, endingCamXMax + num) - endingBaranPosition.position.x;
		BossCutsceneProducer.AlignTransformXs(endingPositions, diffX, flipCondition, x);
		BossCutsceneProducer.AlignTransformXs(endingPositionsWithoutFlip, diffX, flipCondition: false, x);
		BossCutsceneProducer.AlignTransformXs(endingVcamPositions, diffX, flipCondition, x);
		endingVcamPositions.ForEach(delegate(Transform t)
		{
			t.Translate(camDiffX - diffX, 0f, 0f);
		});
	}

	private void StopRain()
	{
		rainVfx.Stop();
		rainSfxEmitter.SetActive(value: false);
	}
}
