using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using Spine.Unity.Playables;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Cutscene;

public class TycheCutsceneProducer : BossCutsceneProducer
{
	[Header("Timeline Assets")]
	[SerializeField]
	private PlayableAsset openingTimeline;

	[SerializeField]
	private PlayableAsset phaseTransitionTimeline;

	[SerializeField]
	private PlayableAsset endingTimeline;

	[Header("Opening")]
	[SerializeField]
	private CinemachineVirtualCamera openingStartCam;

	[SerializeField]
	private CinemachineVirtualCamera openingFinishCam;

	[Header("Phase Transition")]
	[SerializeField]
	private Transform phaseBossPosition;

	[SerializeField]
	private List<Transform> phasePositions;

	[SerializeField]
	private int impulseDurationFrames;

	[SerializeField]
	private float impulseMagnitude;

	[Header("Ending")]
	[SerializeField]
	private CinemachineVirtualCamera endingStartCam;

	[SerializeField]
	private CinemachineVirtualCamera endingFinishCam;

	[SerializeField]
	private Transform endingBossPosition;

	[SerializeField]
	private List<Transform> endingPositions;

	[Header("References")]
	[SerializeField]
	private CutsceneTrigger cutsceneTrigger;

	[SerializeField]
	private List<GameObject> transparentWalls;

	[SerializeField]
	private GameObject jingleEmitter;

	[SerializeField]
	private Renderer treasureBoxRenderer;

	private void LateUpdate()
	{
		ConfinePlayerMovement();
	}

	protected override void SetProducerState(int state)
	{
		switch (state)
		{
		case 0:
		case 1:
		case 3:
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: false);
			});
			cutsceneTrigger.OnTriggered += delegate
			{
				PlayTimelineAsync(0, blockControl: true).Forget();
			};
			jingleEmitter.SetActive(value: true);
			break;
		case 5:
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: false);
			});
			jingleEmitter.SetActive(value: false);
			ActivateCorpse();
			break;
		default:
			jingleEmitter.SetActive(value: false);
			break;
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 0:
			base.PlayableDirector.playableAsset = openingTimeline;
			base.BossSpine.SetAnimation("scene/intro_loop");
			bossFigure.gameObject.SetActive(value: true);
			MoveCutsceneStartCamToCurrentCam(openingStartCam);
			treasureBoxRenderer.sortingLayerName = "Monster";
			BossCutsceneProducer.InvokeAfterWaitPlayerAnim(delegate
			{
				CutsceneProducer.Player.mover.SetAnimation("see_low");
			});
			break;
		case 2:
			base.PlayableDirector.playableAsset = phaseTransitionTimeline;
			AdjustPositionsPhase();
			Boss.IsImmortal = false;
			Boss.gameObject.SetActive(value: true);
			base.PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Spine Animation State Track", Boss.SkeletonAnimation);
			break;
		case 4:
			DOTween.Kill(Boss.transform);
			Boss.Position = new Vector2(Boss.Position.x, Boss.SpawnPosition.y);
			Boss.SetAnimationInstantly("scene/outro_start", loop: false);
			base.PlayableDirector.playableAsset = endingTimeline;
			ClearFight();
			FlipBossToPlayer();
			AdjustPositionsEnding();
			SwitchToBossFigure();
			MoveCutsceneStartCamToCurrentCam(endingStartCam);
			SetConfinePlayerMovement = true;
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
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: true);
			});
			SynchronizeEndVcamAsync(openingFinishCam).Forget();
			PlayerLookIdle();
			StartFight();
			break;
		case 2:
			ExitBossCutsceneState();
			break;
		case 4:
			SynchronizeEndVcamAsync(endingFinishCam).Forget();
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: false);
			});
			CutsceneProducer.Field.ChangeAreaTo(OriginArea);
			EarnMoney();
			SetConfinePlayerMovement = false;
			ShowResonance();
			break;
		case 1:
		case 3:
			break;
		}
	}

	protected override void OnSkipTimeline(int state)
	{
		OnBeforeSkipTimeline();
		if (state == 0)
		{
			ChangeArea();
			StopJingleSound();
			ChangeTreasureLayer();
			PlayerLookIdle();
		}
	}

	public void StopJingleSound()
	{
		jingleEmitter.SetActive(value: false);
	}

	public void ChangeTreasureLayer()
	{
		treasureBoxRenderer.sortingLayerName = "Default";
	}

	public void ShakeCameraPhase()
	{
		ShakeCameraForSeconds(10f, 5f, 1f);
	}

	public void HideBossPhase()
	{
		Boss.IsImmortal = true;
	}

	public void ShowBossPhase()
	{
		Boss.IsImmortal = false;
	}

	public void StompForcePhase()
	{
		ApplyStompForceAgainstBoss(impulseDurationFrames, impulseMagnitude);
	}

	public void PlayerGuard()
	{
		CutsceneProducer.Player.mover.SetAnimation("dagger/guard", loop: false);
		CutsceneProducer.Player.mover.AddAnimation("dagger/guard_loop");
	}

	private void AdjustPositionsPhase()
	{
		float diffX = Boss.transform.position.x - phaseBossPosition.position.x;
		BossCutsceneProducer.AlignTransformXs(phasePositions, diffX, Boss.transform.localScale.x <= 0f, Boss.transform.position.x);
		bossFigure.SynchronizePositionDirection(Boss.transform);
	}

	private void AdjustPositionsEnding()
	{
		float diffX = Boss.transform.position.x - endingBossPosition.position.x;
		BossCutsceneProducer.AlignTransformXs(endingPositions, diffX, Boss.transform.localScale.x < 0f, Boss.transform.position.x);
	}
}
