using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace SPR.Cutscene;

public class GaiaCutsceneProducer : BossCutsceneProducer
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

	[SerializeField]
	private Transform openingPlayerPosition;

	[Header("Phase Transition")]
	[SerializeField]
	private Transform phasePosition;

	[SerializeField]
	private List<Transform> phasePositions;

	[SerializeField]
	private int phaseDef;

	[SerializeField]
	private int forceDurationFrames;

	[SerializeField]
	private int forceMagnitude;

	[Header("Ending")]
	[SerializeField]
	private List<CinemachineVirtualCamera> endingStartCams;

	[FormerlySerializedAs("endingVcamPositions")]
	[SerializeField]
	private List<CinemachineVirtualCamera> endingMovingCams;

	[SerializeField]
	private CinemachineVirtualCamera endingFinishCam;

	[SerializeField]
	private Transform endingBossPosition;

	[SerializeField]
	private List<Transform> endingBossPositions;

	[Header("References")]
	[SerializeField]
	private SkeletonAnimation openingStatue;

	[SerializeField]
	private CutsceneTrigger cutsceneTrigger;

	[SerializeField]
	private GameObject[] transparentWalls;

	[SerializeField]
	private SpineDoor[] doors;

	[Header("Values")]
	[SerializeField]
	private float endingCamXMin;

	[SerializeField]
	private float endingCamXMax;

	private Renderer statueRenderer;

	private Gaia gaia;

	private Gaia Gaia => gaia ?? (gaia = Boss.CastObject<Gaia>());

	private void LateUpdate()
	{
		ConfinePlayerMovement();
	}

	protected override void SetProducerState(int state)
	{
		SetStatueAlive(state == 0 || state == 1 || state == 3);
		if ((uint)state <= 1u || state == 3)
		{
			cutsceneTrigger.OnTriggered += delegate
			{
				PlayTimelineAsync(0, blockControl: true).Forget();
			};
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 0:
			base.PlayableDirector.playableAsset = openingTimeline;
			await UniTask.WaitUntil(() => CutsceneProducer.Player.mover.IsOnGround, PlayerLoopTiming.Update, token);
			doors.ForEach(delegate(SpineDoor d)
			{
				d.SetDoorOpen(open: false);
			});
			MoveCutsceneStartCamToCurrentCam(openingStartCam);
			CutsceneProducer.Player.MoveXPositionAsync(openingPlayerPosition.position.x, isRunning: false, token).Forget();
			SetGaiaSkinPhase2(value: false);
			break;
		case 2:
			base.PlayableDirector.playableAsset = phaseTransitionTimeline;
			AdjustPositionsPhase();
			BindTracksToRealBoss(tween: false, animator: true);
			break;
		case 4:
			SetConfinePlayerMovement = true;
			base.PlayableDirector.playableAsset = endingTimeline;
			Gaia.ClearMark();
			ClearFight();
			FlipBossToPlayer();
			AdjustPositionsEnding();
			SwitchToBossFigure();
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
			SynchronizeEndVcamAsync(openingFinishCam).Forget();
			transparentWalls.ForEach(delegate(GameObject x)
			{
				x.SetActive(value: true);
			});
			StartFight();
			SetStatueAlive(value: false);
			break;
		case 2:
			ExitBossCutsceneState();
			break;
		case 4:
			SetConfinePlayerMovement = false;
			CutsceneProducer.Field.ChangeAreaTo(OriginArea);
			SynchronizeEndVcamAsync(endingFinishCam).Forget();
			transparentWalls.ForEach(delegate(GameObject x)
			{
				x.SetActive(value: false);
			});
			doors.ForEach(delegate(SpineDoor d)
			{
				d.SetDoorOpen(open: true);
			});
			GetReward().Forget();
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
		}
	}

	public void StompForce()
	{
		ApplyStompForceAgainstBoss(forceDurationFrames, forceMagnitude);
	}

	public void ResetHitAnim()
	{
		Gaia.DisablePhaseHitAnim();
	}

	public void ShakeCameraPhase()
	{
		ShakeCameraForSeconds(7.5f, 2f, 0.5f);
	}

	public void SetDefense(bool value)
	{
		Gaia.SetDef(value ? phaseDef : 0);
	}

	public void SetGaiaSkinPhase2(bool value)
	{
		string skin = (value ? "Phase2" : "Gaia");
		Gaia.SetSkin(skin);
		base.BossSpine.Skeleton.SetSkin(skin);
		base.BossSpine.Skeleton.SetSlotsToSetupPose();
	}

	private void AdjustPositionsPhase()
	{
		FlipBossToPlayer();
		bossFigure.SynchronizePositionDirection(Boss.transform);
		foreach (Transform phasePosition in phasePositions)
		{
			phasePosition.position = new Vector3(bossFigure.position.x, phasePosition.position.y);
		}
	}

	private void AdjustPositionsEnding()
	{
		FlipBossToPlayer();
		bossFigure.SynchronizePositionDirection(Boss.transform);
		endingStartCams.ForEach(base.MoveCutsceneStartCamToCurrentCam);
		float diffX = Boss.transform.position.x - endingBossPosition.position.x;
		BossCutsceneProducer.AlignTransformXs(endingBossPositions, diffX, Boss.transform.localScale.x > 0f, Boss.transform.position.x);
		endingMovingCams.ForEach(delegate(CinemachineVirtualCamera vcam)
		{
			vcam.transform.Translate(diffX, 0f, 0f);
		});
	}

	private void SetStatueAlive(bool value)
	{
		if ((object)statueRenderer == null)
		{
			statueRenderer = openingStatue.GetComponent<Renderer>();
		}
		statueRenderer.sortingLayerName = (value ? "Character" : "Default");
		openingStatue.SetAnimation(value ? "idle_start" : "idle_end");
	}

	private async UniTaskVoid GetReward()
	{
		new PureItem("GaiaSpirit").AddToPlayerData(ItemAcquireDisplayOrder.ShowFullPanel);
		await UniTask.WaitWhile(() => CutsceneProducer.FieldCanvas.InteractionPanel.IsDisplaying);
		CutsceneProducer.Player.PlayerStat.AddStatAppliedExperience(Boss.Exp);
		Game.Instance.Player.PlayerPart.OnKill();
		EarnMoney();
		await UniTask.WaitWhile(() => CutsceneProducer.FieldCanvas.LevelBoard.IsLevelUpProducing);
		new PureItem("RoyalSealPremium").AddToPlayerData(ItemAcquireDisplayOrder.ShowNotification);
		ShowResonance();
	}
}
