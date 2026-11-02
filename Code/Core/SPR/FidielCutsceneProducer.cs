using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Cutscene;
using SPR.Field;
using Spine.Unity.Playables;
using Timeline.Samples;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Timeline;

namespace SPR;

public class FidielCutsceneProducer : BossCutsceneProducer
{
	[Header("Timeline Asset")]
	[SerializeField]
	private TimelineAsset openingTimeline;

	[SerializeField]
	private TimelineAsset phaseTransitionTimeline;

	[SerializeField]
	private TimelineAsset endingTimeline;

	[Header("Opening")]
	[SerializeField]
	private CinemachineVirtualCamera openingCam;

	[SerializeField]
	private Transform openingStartPosition;

	[Header("Phase Transition")]
	[SerializeField]
	private Transform phaseBossPosition;

	[SerializeField]
	private List<Transform> phasePositions;

	[SerializeField]
	private CinemachineVirtualCamera phase1Cam;

	[SerializeField]
	private CinemachineVirtualCamera phase2Cam;

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
	private Area phase1Area;

	[SerializeField]
	private Area phase2Area;

	[SerializeField]
	private FakeFloor fakeFloor;

	[SerializeField]
	private List<Door> shutDownDoors;

	[SerializeField]
	private GameObject rightEntrance;

	[SerializeField]
	private ParticleSystem flowerParticle;

	[SerializeField]
	private AreaChanger mainToFloor;

	[SerializeField]
	private GameObject[] environmentObjects;

	private Light2D bossFigureLight;

	private Fidiel Fidiel => Boss.CastObject<Fidiel>();

	private Light2D BossFigureLight => bossFigureLight ?? (bossFigureLight = bossFigure.GetComponentInChildren<Light2D>());

	protected override void SetProducerState(int state)
	{
		rightEntrance.SetActive(value: false);
		switch (state)
		{
		case 0:
		case 1:
		case 3:
			cutsceneTrigger.OnTriggered += delegate
			{
				PlayTimelineAsync(0, blockControl: true).Forget();
			};
			bossFigure.SynchronizePositionDirection(openingStartPosition);
			base.BossSpine.SetAnimationInstantly("scene/ready");
			BossFigureLight.intensity = 0f;
			bossFigure.gameObject.SetActive(value: true);
			shutDownDoors.ForEach(delegate(Door door)
			{
				door.SetDoorOpen(open: true);
			});
			fakeFloor.SetActive(value: true);
			break;
		case 4:
			shutDownDoors.ForEach(delegate(Door door)
			{
				door.SetDoorOpen(open: false);
			});
			break;
		case 5:
			fakeFloor.SetActive(value: false);
			rightEntrance.SetActive(value: true);
			bossFigure.gameObject.SetActive(value: false);
			environmentObjects.ForEach(delegate(GameObject o)
			{
				o.SetActive(value: false);
			});
			mainToFloor.gameObject.SetActive(value: true);
			flowerParticle.Stop();
			shutDownDoors.ForEach(delegate(Door door)
			{
				door.gameObject.SetActive(value: false);
			});
			ActivateCorpse();
			break;
		case 2:
			break;
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 0:
			base.PlayableDirector.playableAsset = openingTimeline;
			CutsceneProducer.Player.MoveXPositionAsync(421f, isRunning: false, token).Forget();
			await BlendFieldCamToCutsceneStartCamAsync(openingCam, 1f);
			phase1Cam.gameObject.SetActive(value: true);
			break;
		case 2:
			base.PlayableDirector.playableAsset = phaseTransitionTimeline;
			AdjustPositionsPhase();
			Boss.IsImmortal = false;
			Boss.gameObject.SetActive(value: true);
			base.PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Spine Animation State Track", Boss.SkeletonAnimation);
			base.PlayableDirector.TrackBinding<TweenTrack>("Tween Track", Boss.transform);
			phase1Cam.gameObject.SetActive(value: false);
			phase2Cam.gameObject.SetActive(value: true);
			phase2Cam.Follow = Game.Instance.Player.transform;
			Fidiel.RebindGroggyVCam(phase2Cam);
			break;
		case 4:
			base.PlayableDirector.playableAsset = endingTimeline;
			ClearFight();
			FlipBossToPlayer();
			AdjustPositionsEnding();
			SwitchToBossFigure();
			MoveCutsceneStartCamToCurrentCam(endingStartCam);
			break;
		case 1:
		case 3:
			break;
		}
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
			shutDownDoors.ForEach(delegate(Door door)
			{
				door.SetDoorOpen(open: false);
			});
			SynchronizeEndVcamAsync(openingCam).Forget();
			StartFight();
			Fidiel.SetArea(phase1Area);
			break;
		case 2:
			shutDownDoors.ForEach(delegate(Door door)
			{
				door.SetDoorOpen(open: false);
			});
			ExitBossCutsceneState();
			Fidiel.SetArea(phase2Area);
			Fidiel.SetGroundPosition();
			break;
		case 4:
			shutDownDoors.ForEach(delegate(Door door)
			{
				door.SetDoorOpen(open: true);
			});
			rightEntrance.SetActive(value: true);
			CutsceneProducer.Field.ChangeAreaTo(rightEntrance.GetComponent<Entrance>().ConnectArea);
			SynchronizeEndVcamAsync(endingFinishCam).Forget();
			CutsceneProducer.SoundManager.StopAMB();
			flowerParticle.Stop();
			EarnMoney();
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
			PlayerLookIdle();
		}
	}

	public void CloseDoor()
	{
		shutDownDoors.ForEach(delegate(Door door)
		{
			door.SetDoorOpen(open: false);
		});
	}

	public void SpawnThrowSpear(float speed)
	{
		Fidiel.StrongJumpSpearThrow(speed);
	}

	private void AdjustPositionsPhase()
	{
		Direction direction = ((!(Boss.VisualBottom.x > base.FightArea.AreaBox.Center.x)) ? Direction.Right : Direction.Left);
		Boss.Flip(direction);
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
