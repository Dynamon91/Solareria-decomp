using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Cutscene;
using SPR.Field;
using Spine.Unity.Playables;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public class GatekeeperCutsceneProducer : BossCutsceneProducer
{
	[Header("Timelines")]
	[SerializeField]
	private PlayableAsset openingTimeline;

	[SerializeField]
	private PlayableAsset phaseTransitionTimeline;

	[SerializeField]
	private PlayableAsset endingTimeline;

	[Header("Opening")]
	[SerializeField]
	private CinemachineVirtualCamera openingCam;

	[SerializeField]
	private Transform openingLandPosition;

	[Header("Phase Transition")]
	[SerializeField]
	private CinemachineVirtualCamera phaseCam;

	[SerializeField]
	private Transform phaseGatekeeperPosition;

	[SerializeField]
	private List<Transform> phasePositions;

	[SerializeField]
	private Vector2 knockBackVelocity;

	[Header("Ending")]
	[SerializeField]
	private CinemachineVirtualCamera endingStartCam;

	[SerializeField]
	private CinemachineVirtualCamera endingFinishCam;

	[SerializeField]
	private Transform endingGatekeeperPosition;

	[SerializeField]
	private List<Transform> endingPositions;

	[SerializeField]
	private List<CinemachineVirtualCamera> endingVcams;

	[Header("References")]
	[SerializeField]
	private CutsceneTrigger cutsceneTrigger;

	[SerializeField]
	private List<GameObject> transparentWalls;

	protected override Vector2 KnockBackVelocity => knockBackVelocity;

	private void Awake()
	{
		cutsceneTrigger.OnTriggered += delegate
		{
			PlayTimelineAsync(0, blockControl: true).Forget();
		};
	}

	protected override void SetProducerState(int state)
	{
		switch (state)
		{
		case 0:
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: false);
			});
			break;
		case 5:
			cutsceneTrigger.gameObject.SetActive(value: false);
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
			BossCutsceneProducer.InvokeAfterWaitPlayerAnim(base.PlayerLookUp);
			break;
		case 2:
			base.PlayableDirector.playableAsset = phaseTransitionTimeline;
			AdjustPositionsPhase();
			Boss.IsImmortal = false;
			Boss.gameObject.SetActive(value: true);
			base.PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Spine Animation State Track", Boss.SkeletonAnimation);
			break;
		case 4:
			base.PlayableDirector.playableAsset = endingTimeline;
			ClearFight();
			FlipBossToPlayer();
			AdjustPositionsEnding();
			SwitchToBossFigure();
			MoveCutsceneStartCamToCurrentCam(endingStartCam);
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
			Boss.transform.position = openingLandPosition.position;
			SynchronizeEndVcamAsync(openingCam).Forget();
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: true);
			});
			StartFight();
			break;
		case 2:
			ExitBossCutsceneState();
			break;
		case 4:
		{
			bossFigure.gameObject.SetActive(value: false);
			SynchronizeEndVcamAsync(endingFinishCam).Forget();
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: false);
			});
			CutsceneProducer.Field.ChangeAreaTo(OriginArea);
			EarnMoney();
			Vector2 startPos = new Vector2(Boss.transform.position.x, Boss.SpawnPosition.y + 1f);
			Vector2 v = new Vector2(Boss.transform.position.x, Boss.SpawnPosition.y + 1f) - Boss.FacingDirection.ToVector2();
			ItemObject.DropItemAsync("MucoraPortalDoorKey", startPos, v.ToVector2Int()).Forget();
			ShowResonance();
			break;
		}
		case 1:
		case 3:
			break;
		}
	}

	protected override void OnSkipTimeline(int state)
	{
		if (bossFigure.gameObject.activeSelf)
		{
			OnBeforeSkipTimeline();
		}
		if (state == 0)
		{
			ChangeArea();
		}
	}

	public void ShakeCameraLightening()
	{
		ShakeCameraForSeconds(5f, 2f, 0.5f);
	}

	private void AdjustPositionsPhase()
	{
		float diffX = Boss.transform.position.x - phaseGatekeeperPosition.position.x;
		BossCutsceneProducer.AlignTransformXs(phasePositions, diffX, Boss.transform.localScale.x > 0f, Boss.transform.position.x);
		bossFigure.SynchronizePositionDirection(Boss.transform);
	}

	private void AdjustPositionsEnding()
	{
		bool flipCondition = Boss.transform.localScale.x > 0f;
		float x = Boss.transform.position.x;
		float diffX = Boss.transform.position.x - endingGatekeeperPosition.position.x;
		BossCutsceneProducer.AlignTransformXs(endingPositions, diffX, flipCondition, x);
		BossCutsceneProducer.AlignTransformXs(endingVcams.Select((CinemachineVirtualCamera vcam) => vcam.transform), diffX, flipCondition, x);
		endingVcams.ForEach(delegate(CinemachineVirtualCamera vcam)
		{
			vcam.transform.position = base.FightArea.ClampCamera(vcam);
		});
	}
}
