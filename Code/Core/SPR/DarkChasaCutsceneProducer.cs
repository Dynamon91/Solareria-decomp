using System;
using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using SPR.Cutscene;
using SPR.UI;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;

namespace SPR;

public class DarkChasaCutsceneProducer : BossCutsceneProducer
{
	[Serializable]
	private struct LightSquare
	{
		public SpriteRenderer renderer;

		public Color color;

		public void SetColor()
		{
			renderer.color = color;
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

	[SerializeField]
	private PlayableAsset funeralTimeline;

	[Header("Opening")]
	[SerializeField]
	private CinemachineVirtualCamera openingFollowCam;

	[SerializeField]
	private CinemachineVirtualCamera openingStartCam;

	[SerializeField]
	private CinemachineVirtualCamera openingFinishCam;

	[SerializeField]
	private Transform openingPlayerPosition;

	[SerializeField]
	private Transform openingBossPosition;

	[Header("Phase Two Transition")]
	[SerializeField]
	private Transform phaseTwoLeftPosition;

	[SerializeField]
	private Transform phaseTwoRightPosition;

	[SerializeField]
	private Transform phaseTwoStartPosition;

	[SerializeField]
	private Transform phaseTwoEndPosition;

	[SerializeField]
	private List<Transform> phaseTwoStartPositions;

	[SerializeField]
	private List<Transform> phaseTwoEndPositions;

	[SerializeField]
	private int forceDurationFrames;

	[SerializeField]
	private float forceMagnitude;

	[Header("Phase Three Transition")]
	[SerializeField]
	private CinemachineVirtualCamera phaseThreeStartCam;

	[SerializeField]
	private CinemachineVirtualCamera phaseThreeMoveCam;

	[SerializeField]
	private Transform phaseThreePosition;

	[SerializeField]
	private List<Transform> phaseThreePositions;

	[SerializeField]
	private float phaseThreeDistance;

	[SerializeField]
	private Vector2 knockBackVelocity;

	[Header("Ending")]
	[SerializeField]
	private CinemachineVirtualCamera endingStartCam;

	[SerializeField]
	private CinemachineVirtualCamera endingFinishCam;

	[SerializeField]
	private Transform endingBossPosition;

	[SerializeField]
	private Transform endingPlayerPosition;

	[SerializeField]
	private List<Transform> endingPositions;

	[Header("Funeral")]
	[SerializeField]
	private float fadeDuration;

	[SerializeField]
	private CinemachineVirtualCamera funeralEndCam;

	[SerializeField]
	private Transform funeralBossTransform;

	[SerializeField]
	private Transform funeralPlayerTransform;

	[SerializeField]
	private Transform chasaFigure;

	[SerializeField]
	private List<Transform> funeralPositions;

	[Header("References")]
	[SerializeField]
	private Transform fakePlayer;

	[SerializeField]
	private CutsceneTrigger cutsceneTrigger;

	[SerializeField]
	private List<GameObject> transparentWalls;

	[SerializeField]
	private CutsceneInteraction cutsceneInteraction;

	[SerializeField]
	private GameObject entranceBlock;

	[SerializeField]
	private GameObject entrance;

	[SerializeField]
	private GameObject surroundingLight;

	[SerializeField]
	private List<LightSquare> lightSquares;

	[SerializeField]
	private ParticleSystem rainParticle;

	[SerializeField]
	private Light2D layingDarkChasaLight;

	[SerializeField]
	private Animator entranceLight;

	[Header("Ending Narration")]
	[SerializeField]
	private NarrationBoard narrationBoard;

	[SerializeField]
	private string narrationID;

	[SerializeField]
	private float narrationFadeDuration;

	[SerializeField]
	private float narrationTextDuration;

	[Header("SFX")]
	[SerializeField]
	private float rainBattleVolume = 5f;

	[SerializeField]
	private float rainStoryVolume = 1.5f;

	private DarkChasa darkChasa;

	private SkeletonAnimation fakePlayerSpine;

	private StudioEventEmitter rainEmitter;

	private DarkChasa DarkChasa => darkChasa ?? (darkChasa = Boss.CastObject<DarkChasa>());

	private SkeletonAnimation FakePlayerSpine => fakePlayerSpine ?? (fakePlayerSpine = fakePlayer.GetComponentInChildren<SkeletonAnimation>(includeInactive: true));

	protected override Vector2 KnockBackVelocity => knockBackVelocity;

	private StudioEventEmitter RainEmitter => rainEmitter ?? (rainEmitter = rainParticle.GetComponentInChildren<StudioEventEmitter>(includeInactive: true));

	protected override void SetProducerState(int state)
	{
		switch (state)
		{
		case 0:
			bossFigure.gameObject.SetActive(value: true);
			fakePlayer.gameObject.SetActive(value: false);
			base.BossSpine.SetAnimationInstantly("scene/intro_idle");
			cutsceneTrigger.OnTriggered += delegate
			{
				cutsceneInteraction.AwakeObject(Game.Instance.SaveData);
				cutsceneInteraction.gameObject.SetActive(value: true);
				UniTask.WaitUntil(() => cutsceneInteraction.IsInteractionCompleted()).GetAwaiter().OnCompleted(delegate
				{
					CutsceneProducer.InputManager.BlockControlForTask(StartDarkChasaCutsceneAsync(), "Start Dark Chasa Opening").Forget();
				});
			};
			SetEntranceActive(value: false);
			break;
		case 1:
		case 3:
		case 5:
			bossFigure.gameObject.SetActive(value: true);
			bossFigure.SynchronizePositionDirection(openingBossPosition);
			fakePlayer.gameObject.SetActive(value: false);
			base.BossSpine.SetAnimationInstantly("idle");
			lightSquares.ForEach(delegate(LightSquare s)
			{
				s.SetColor();
			});
			cutsceneTrigger.OnTriggered += delegate
			{
				ChangeArea();
				EndOpeningStateTimeline();
				CutsceneProducer.FieldCanvas.BossBoard.ShowAsync().Forget();
			};
			SetEntranceActive(value: false);
			break;
		case 6:
		case 7:
			bossFigure.gameObject.SetActive(value: false);
			fakePlayer.gameObject.SetActive(value: false);
			ActivateCorpse(activateChildren: true);
			ActivateFxsAfterClear();
			SetEntranceActive(value: true);
			layingDarkChasaLight.intensity = 0f;
			break;
		case 8:
			bossFigure.gameObject.SetActive(value: false);
			fakePlayer.gameObject.SetActive(value: false);
			ActivateCorpse(activateChildren: true);
			ActivateFxsAfterClear();
			SetEntranceActive(value: true);
			entranceLight.gameObject.SetActive(value: true);
			entranceLight.Play("start");
			layingDarkChasaLight.intensity = 0f;
			break;
		case 2:
		case 4:
			break;
		}
	}

	private async UniTask StartDarkChasaCutsceneAsync()
	{
		MoveCutsceneStartCamToCurrentCam(openingFollowCam);
		SustainVcamForFade(openingFollowCam);
		CutsceneProducer.FieldCanvas.HideAllAsync(instant: false).Forget();
		await UniTask.WaitUntil(() => CutsceneProducer.Player.mover.IsOnGround);
		await CutsceneProducer.Player.MoveXPositionAsync(openingPlayerPosition.position.x, isRunning: false);
		CutsceneProducer.Player.mover.Flip(Direction.Right);
		CutsceneProducer.Player.mover.SetAnimation("idle");
		openingFollowCam.gameObject.SetActive(value: false);
		PlayTimelineAsync(0, blockControl: true).Forget();
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 0:
			base.PlayableDirector.playableAsset = openingTimeline;
			SwitchToFakePlayerFigure();
			openingPlayerPosition.SynchronizePositionDirection(fakePlayer);
			await BlendFieldCamToCutsceneStartCamAsync(openingStartCam, 1f);
			break;
		case 2:
			base.PlayableDirector.playableAsset = phaseTwoTimeline;
			AdjustPositionsPhaseTwo();
			BindTracksToRealBoss();
			break;
		case 4:
			base.PlayableDirector.playableAsset = phaseThreeTimeline;
			AdjustPositionsPhaseThree();
			SwitchToBossFigure();
			MoveCutsceneStartCamToCurrentCam(phaseThreeStartCam);
			break;
		case 6:
			base.PlayableDirector.playableAsset = endingTimeline;
			ClearFight();
			FlipBossToPlayer();
			AdjustPositionsEnding();
			SwitchToBossFigure();
			MoveCutsceneStartCamToCurrentCam(endingStartCam);
			transparentWalls.ForEach(delegate(GameObject x)
			{
				x.SetActive(value: false);
			});
			RainEmitter.EventInstance.setVolume(rainStoryVolume);
			break;
		case 7:
			fakePlayer.gameObject.SetActive(value: false);
			base.PlayableDirector.playableAsset = funeralTimeline;
			CutsceneProducer.FadeCanvas.FadeIn(fadeDuration).Forget();
			AdjustPositionsFuneral();
			endingFinishCam.gameObject.SetActive(value: false);
			CutsceneProducer.Player.mover.SetAnimation("idle_back");
			CutsceneProducer.SoundManager.PlayBGM("DarkChasaMemorial");
			break;
		}
		await UniTask.CompletedTask;
	}

	protected override void OnEndTimeline(int state)
	{
		switch (state)
		{
		case 0:
			SwitchToRealPlayer();
			EndOpeningStateTimeline();
			SynchronizeEndVcamAsync(openingFinishCam).Forget();
			break;
		case 2:
			ExitBossCutsceneState();
			break;
		case 4:
			PlayerLookIdle();
			SwitchToRealBoss();
			Boss.SetAnimation("phase3_idle");
			SynchronizeEndVcamAsync(phaseThreeStartCam).Forget();
			break;
		case 6:
			SustainVcamForFade(endingFinishCam);
			Game.Instance.LoadFieldAsync(SceneDataModel.MoveCutsceneModel(new FieldID(SectorName.AbandonedTunnels, 21), "DarkChasaFlashback")).Forget();
			break;
		case 7:
			CutsceneProducer.InputManager.BlockControlForTask(AfterCutsceneAsync(), "Set After Dark Chasa Cutscene").Forget();
			GetReward().Forget();
			break;
		case 1:
		case 3:
		case 5:
			break;
		}
	}

	private void EndOpeningStateTimeline()
	{
		SwitchToRealBoss();
		transparentWalls.ForEach(delegate(GameObject x)
		{
			x.SetActive(value: true);
		});
		StartFight();
	}

	protected override void OnSkipTimeline(int state)
	{
		OnBeforeSkipTimeline();
		switch (state)
		{
		case 0:
			ChangeArea();
			break;
		case 4:
			SetRainBattleVolume();
			break;
		case 6:
			DisableFadeInAfterSkip();
			break;
		}
	}

	private async UniTask PlayFuneralTimelineAsync()
	{
		await CutsceneProducer.FadeCanvas.FadeOut(1f);
		await UniTask.WaitForSeconds(fadeDuration);
		PlayTimelineAsync(7, blockControl: true).Forget();
	}

	private async UniTask AfterCutsceneAsync()
	{
		CutsceneProducer.Player.mover.SetAnimation("idle");
		CutsceneProducer.Field.ChangeAreaTo(CutsceneProducer.Field.MainArea);
		SynchronizeEndVcamAsync(funeralEndCam).Forget();
		await UniTask.WaitForSeconds(fadeDuration);
		await CutsceneProducer.FadeCanvas.FadeIn(1f);
		SetEntranceActive(value: true);
	}

	private async UniTaskVoid GetReward()
	{
		new PureItem("DarkChasaSpirit").AddToPlayerData(ItemAcquireDisplayOrder.ShowFullPanel);
		await UniTask.WaitWhile(() => CutsceneProducer.FieldCanvas.InteractionPanel.IsDisplaying);
		CutsceneProducer.Player.PlayerStat.AddStatAppliedExperience(Boss.Exp);
		Game.Instance.Player.PlayerPart.OnKill();
		EarnMoney();
		await UniTask.WaitWhile(() => CutsceneProducer.FieldCanvas.LevelBoard.IsLevelUpProducing);
		new PureItem("RoyalSealPremium").AddToPlayerData(ItemAcquireDisplayOrder.ShowNotification);
		entranceLight.gameObject.SetActive(value: true);
		entranceLight.Play("start");
	}

	public override void EnterLoop(int index)
	{
		if (base.CurrentState == 7)
		{
			StartScenarioInsideCutscene(index);
		}
	}

	public override bool ShouldLoop(int index)
	{
		if (base.CurrentState == 7)
		{
			return CutsceneProducer.IsTalking;
		}
		return false;
	}

	public void FlipBossFigure()
	{
		Vector3 localScale = bossFigure.localScale;
		bossFigure.localScale = new Vector3(0f - localScale.x, localScale.y, localScale.z);
	}

	public void StompForce()
	{
		ApplyStompForceAgainstBoss(forceDurationFrames, forceMagnitude);
	}

	public void MovePlayerPhaseThree()
	{
		Camera main = Camera.main;
		float num = phaseThreeMoveCam.m_Lens.OrthographicSize * (float)main.pixelWidth / (float)main.pixelHeight;
		float x = phaseThreeMoveCam.transform.position.x;
		float x2 = CutsceneProducer.Player.mover.VisualBottom.x;
		if (!(x2 < x - num / 2f) && !(x2 > x + num / 2f))
		{
			float flipDirectionPhaseThree = GetFlipDirectionPhaseThree(bossFigure);
			MovePlayerAndLookUpAsync(bossFigure.position.x + flipDirectionPhaseThree * phaseThreeDistance).Forget();
		}
	}

	public void MovePlayerEnding()
	{
		CutsceneProducer.InputManager.StackControlTarget(this).Forget();
		MovePlayerAndLookUpAsync(endingPlayerPosition.position.x).Forget();
	}

	private async UniTask MovePlayerAndLookUpAsync(float target, bool isRunning = true)
	{
		await CutsceneProducer.Player.MoveXPositionAsync(target, isRunning);
		await BossCutsceneProducer.WaitPlayerAnim();
		FlipPlayerToFigure();
		PlayerLookUp();
	}

	public void FadeOut()
	{
		CutsceneProducer.FadeCanvas.FadeOut(1f).Forget();
	}

	public void SetRainBattleVolume()
	{
		rainParticle.gameObject.SetActive(value: true);
		rainParticle.Play();
		RainEmitter.EventInstance.setVolume(rainBattleVolume);
	}

	public void ShowFadeNarration()
	{
		ScenarioData scenarioData = DataManager.Instance.GetScenarioData(narrationID);
		narrationBoard.SetScenario(scenarioData);
		narrationBoard.SetActiveDuration(narrationTextDuration, fadeDuration);
		CutsceneProducer.SoundManager.PlayBGM("ChasaRiver");
	}

	private void AdjustPositionsPhaseTwo()
	{
		float num = (phaseTwoLeftPosition.position.x + phaseTwoRightPosition.position.x) / 2f;
		Transform transform = ((CutsceneProducer.Player.mover.VisualBottom.x >= num) ? phaseTwoLeftPosition : phaseTwoRightPosition);
		float diffX = Boss.transform.position.x - phaseTwoStartPosition.position.x;
		BossCutsceneProducer.AlignTransformXs(phaseTwoStartPositions, diffX, Boss.transform.localScale.x > 0f, Boss.transform.position.x);
		float diffX2 = transform.position.x - phaseTwoEndPosition.position.x;
		BossCutsceneProducer.AlignTransformXs(phaseTwoEndPositions, diffX2, transform.localScale.x * phaseTwoEndPosition.localScale.x < 0f, transform.position.x);
		Boss.Flip(transform.localScale.x);
	}

	private void AdjustPositionsPhaseThree()
	{
		Boss.Flip(GetFlipDirectionPhaseThree(Boss.transform));
		float diffX = Boss.transform.position.x - phaseThreePosition.transform.position.x;
		BossCutsceneProducer.AlignTransformXs(phaseThreePositions, diffX, Boss.transform.localScale.x > 0f, Boss.transform.position.x);
	}

	private void AdjustPositionsEnding()
	{
		float diffX = Boss.transform.position.x - endingBossPosition.position.x;
		BossCutsceneProducer.AlignTransformXs(endingPositions, diffX, Boss.transform.localScale.x > 0f, Boss.transform.position.x);
	}

	private void AdjustPositionsFuneral()
	{
		Vector3 position = funeralBossTransform.position;
		float diffX = Mathf.Abs(funeralPositions[0].position.x - funeralPositions[1].position.x) / 2f;
		(position.x - funeralPositions[0].position.x).GetSign();
		int sign = Boss.transform.localScale.x.GetSign();
		float flipOrigin = position.x + 1f;
		BossCutsceneProducer.AlignTransformXs(funeralPositions, diffX, sign > 0, flipOrigin);
		CutsceneProducer.Player.SetPosition(funeralPlayerTransform.position, sign <= 0);
		if (sign > 0)
		{
			chasaFigure.Flip(-chasaFigure.localScale.x.GetSign());
		}
	}

	private void SwitchToFakePlayerFigure()
	{
		fakePlayer.SynchronizePositionDirection(CutsceneProducer.Player.transform);
		FakePlayerSpine.SynchronizeAnimation(CutsceneProducer.Player.mover.SkeletonAnimation);
		CutsceneProducer.Player.SetPosition(Util.DisablePosition);
		fakePlayer.gameObject.SetActive(value: true);
	}

	private void SwitchToRealPlayer()
	{
		CutsceneProducer.Player.mover.SynchronizeAnimation(FakePlayerSpine);
		SynchronizePlayer(fakePlayer);
	}

	private float GetFlipDirectionPhaseThree(Transform bossTransform)
	{
		return (base.FightArea.AreaBox.Center.x > bossTransform.position.x) ? 1 : (-1);
	}

	private void SetEntranceActive(bool value)
	{
		entrance.SetActive(value);
		entranceBlock.SetActive(!value);
		surroundingLight.SetActive(!value);
	}

	private void ActivateFxsAfterClear()
	{
		lightSquares.ForEach(delegate(LightSquare s)
		{
			s.SetColor();
		});
		ParticleSystem.MainModule main = rainParticle.main;
		main.prewarm = true;
		rainParticle.gameObject.SetActive(value: true);
		rainParticle.Play();
		RainEmitter.EventInstance.setVolume(rainStoryVolume);
	}
}
