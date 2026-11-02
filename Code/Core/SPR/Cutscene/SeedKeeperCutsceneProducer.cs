using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Cutscene;

public class SeedKeeperCutsceneProducer : BossCutsceneProducer
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
	private CinemachineVirtualCamera openingStartVcam;

	[SerializeField]
	private CinemachineVirtualCamera openingEndVcam;

	[SerializeField]
	private float walkingDestinationX;

	[Header("Phase Transition")]
	[SerializeField]
	private CinemachineVirtualCamera phaseEndVcam;

	[SerializeField]
	private List<Transform> ptJumpUpTransforms;

	[SerializeField]
	private Transform jumpDustTransform;

	[Header("Ending")]
	[SerializeField]
	private CinemachineVirtualCamera endingEndVcam;

	[SerializeField]
	private Transform endingBossPosition;

	[SerializeField]
	private List<Transform> endingPositionFollowers;

	[SerializeField]
	private List<TumblableObject> tumblableObjects;

	[Header("References")]
	[SerializeField]
	private List<GameObject> transparentWalls;

	[SerializeField]
	private List<CutsceneTrigger> targetTriggers;

	private SeedKeeper seedKeeper;

	private SeedKeeper SeedKeeper => seedKeeper ?? (seedKeeper = base.FightArea.GetComponentInChildren<SeedKeeper>(includeInactive: true));

	private void Awake()
	{
		targetTriggers.ForEach(delegate(CutsceneTrigger t)
		{
			t.OnTriggered += CheckCutsceneTrigger;
		});
	}

	protected override void SetProducerState(int state)
	{
		switch (state)
		{
		case 0:
		case 1:
			bossFigure.gameObject.SetActive(value: false);
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: false);
			});
			break;
		case 4:
			base.gameObject.SetActive(value: false);
			break;
		case 2:
		case 3:
			break;
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 0:
		case 1:
			base.PlayableDirector.playableAsset = openingTimeline;
			CutsceneProducer.Player.MoveXPositionAsync(walkingDestinationX, isRunning: false, token).Forget();
			await BlendFieldCamToCutsceneStartCamAsync(openingStartVcam, 1f);
			bossFigure.gameObject.SetActive(value: true);
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: true);
			});
			OriginArea = CutsceneProducer.Field.CurrentArea;
			break;
		case 2:
		{
			base.PlayableDirector.playableAsset = phaseTransitionTimeline;
			foreach (Transform ptJumpUpTransform in ptJumpUpTransforms)
			{
				Vector3 position = ptJumpUpTransform.position;
				position.x = SeedKeeper.VisualCenter.x;
				Vector3 localScale = ptJumpUpTransform.localScale;
				localScale.x *= ((!(SeedKeeper.transform.localScale.x < 0f)) ? 1 : (-1));
				ptJumpUpTransform.position = position;
				ptJumpUpTransform.rotation = Quaternion.Euler(localScale);
			}
			Vector3 position2 = jumpDustTransform.position;
			position2.x = SeedKeeper.VisualCenter.x;
			jumpDustTransform.position = position2;
			bossFigure.gameObject.SetActive(value: true);
			break;
		}
		case 3:
			base.PlayableDirector.playableAsset = endingTimeline;
			ClearFight();
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
		case 1:
			SynchronizeEndVcamAsync(openingEndVcam).Forget();
			SeedKeeper.SynchronizeAnimation(base.BossSpine);
			bossFigure.gameObject.SetActive(value: false);
			SeedKeeper.ExitCutsceneState(bossFigure.gameObject.transform);
			StartFight();
			break;
		case 2:
			SynchronizeEndVcamAsync(phaseEndVcam).Forget();
			SeedKeeper.SynchronizeAnimation(base.BossSpine);
			bossFigure.gameObject.SetActive(value: false);
			SeedKeeper.ExitCutsceneState(bossFigure.gameObject.transform);
			break;
		case 3:
			CutsceneProducer.Field.ChangeAreaTo(OriginArea);
			SynchronizeEndVcamAsync(endingEndVcam).Forget();
			bossFigure.gameObject.SetActive(value: false);
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: false);
			});
			EarnMoney();
			ShowResonance();
			break;
		}
	}

	protected override void OnSkipTimeline(int state)
	{
		OnBeforeSkipTimeline();
		switch (state)
		{
		case 0:
			ChangeArea();
			break;
		case 3:
			ExplodeArmor();
			break;
		}
	}

	public override bool ShouldLoop(int index)
	{
		if (base.CurrentState == 0)
		{
			return CutsceneProducer.Player.mover.VisualBottom.x < walkingDestinationX;
		}
		return false;
	}

	public void CheckCutsceneTrigger()
	{
		if (targetTriggers.All((CutsceneTrigger trigger) => trigger.IsTriggered))
		{
			PlayTimelineAsync(0, blockControl: true).Forget();
		}
	}

	public void ExplodeArmor()
	{
		tumblableObjects.ForEach(delegate(TumblableObject to)
		{
			to.Explode();
		});
		CutsceneProducer.SoundManager.PlayBGM("SeedKeeperOrgel");
	}

	private void AdjustPositionsEnding()
	{
		FlipBossToTarget(base.FightArea.AreaBox.Center.x);
		float x = Boss.transform.position.x;
		bool flag = Boss.transform.localScale.x > 0f;
		DebugLog.GeneralLog($"target = {base.FightArea.AreaBox.Center.x}, origin = {x}, condition = {flag}");
		float diffX = Boss.transform.position.x - endingBossPosition.position.x;
		BossCutsceneProducer.AlignTransformXs(endingPositionFollowers, diffX, flag, x);
	}
}
