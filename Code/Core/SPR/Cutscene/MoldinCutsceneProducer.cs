using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.UI.Map;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Cutscene;

public class MoldinCutsceneProducer : BossCutsceneProducer
{
	[Header("Timeline Assets")]
	[SerializeField]
	private PlayableAsset openingTimeline;

	[SerializeField]
	private PlayableAsset retryTimeline;

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

	[Header("Retry")]
	[SerializeField]
	private CinemachineVirtualCamera retryStartCam;

	[SerializeField]
	private CinemachineVirtualCamera retryFinishCam;

	[Header("Phase Transition")]
	[SerializeField]
	private Transform phasePosition;

	[SerializeField]
	private List<Transform> phasePositions;

	[SerializeField]
	private int forceDurationFrames;

	[SerializeField]
	private int forceMagnitude;

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
	private Transform endingPlayerCenterPosition;

	[SerializeField]
	private List<Transform> endingBossPositions;

	[SerializeField]
	private List<Transform> endingPlayerPositionsWithoutFlip;

	[SerializeField]
	private float playerWalkBackDistance;

	[Header("References")]
	[SerializeField]
	private Transform corFigure;

	[SerializeField]
	private CutsceneTrigger cutsceneTrigger;

	[SerializeField]
	private List<GameObject> transparentWalls;

	[SerializeField]
	private List<GameObject> openingObjects;

	[SerializeField]
	private SpecialTalkTrigger specialTalkTrigger;

	private GameObject corNpc;

	private bool isVeritaTalking;

	private float walkBackOffset;

	public override bool IsProducing
	{
		get
		{
			if (!base.IsProducing)
			{
				return isVeritaTalking;
			}
			return true;
		}
	}

	protected override List<int> UnskippableCutscenes => new List<int> { 1 };

	private Moldin Moldin => Boss.CastObject<Moldin>();

	private GameObject CorNpc => corNpc ?? (corNpc = base.transform.parent.GetComponentInChildren<Cor>(includeInactive: true).gameObject);

	protected override void SetProducerState(int state)
	{
		switch (state)
		{
		case 0:
			cutsceneTrigger.OnTriggered += delegate
			{
				PlayTimelineAsync(0, blockControl: true).Forget();
			};
			CorNpc.SetActive(value: true);
			openingObjects.ForEach(delegate(GameObject obj)
			{
				obj.SetActive(value: true);
			});
			break;
		case 1:
		case 2:
		case 3:
		case 4:
			cutsceneTrigger.OnTriggered += delegate
			{
				PlayTimelineAsync(1, blockControl: true).Forget();
			};
			CorNpc.SetActive(value: false);
			openingObjects.ForEach(delegate(GameObject obj)
			{
				obj.SetActive(value: false);
			});
			break;
		case 6:
			bossFigure.gameObject.SetActive(value: false);
			CorNpc.SetActive(value: false);
			openingObjects.ForEach(delegate(GameObject obj)
			{
				obj.SetActive(value: false);
			});
			base.PlayableDirector.playableAsset = endingTimeline;
			base.PlayableDirector.time = endingTimeline.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.Stop();
			ActivateCorpse();
			break;
		case 5:
			break;
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 0:
			base.PlayableDirector.playableAsset = openingTimeline;
			await UniTask.WaitUntil(() => CutsceneProducer.Player.mover.IsOnGround);
			MoveCutsceneStartCamToCurrentCam(openingStartCam);
			CutsceneProducer.Player.MoveXPositionAsync(openingPlayerPosition.position.x, isRunning: false, token).GetAwaiter().OnCompleted(base.LookUpAfterIdle);
			break;
		case 1:
			base.PlayableDirector.playableAsset = retryTimeline;
			MoveCutsceneStartCamToCurrentCam(retryStartCam);
			break;
		case 3:
			base.PlayableDirector.playableAsset = phaseTransitionTimeline;
			Moldin.WeaponDisappear();
			AdjustPositionsPhase();
			BindTracksToRealBoss(tween: false);
			break;
		case 5:
			base.PlayableDirector.playableAsset = endingTimeline;
			ClearFight();
			MenuMapPanel.AddInfoStoneInformation("Earthford_28_Cor");
			FlipBossToPlayer();
			AdjustPositionsEnding();
			SwitchToBossFigure();
			MoveCutsceneStartCamToCurrentCam(endingStartCam);
			transparentWalls.ForEach(delegate(GameObject x)
			{
				x.SetActive(value: false);
			});
			CutsceneProducer.SoundManager.PlayAMB("MucoraMoldin");
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
			SwitchToRealBoss();
			SynchronizeEndVcamAsync(openingFinishCam).Forget();
			transparentWalls.ForEach(delegate(GameObject x)
			{
				x.SetActive(value: true);
			});
			StartFight();
			corNpc.SetActive(value: false);
			break;
		case 3:
			ExitBossCutsceneState();
			break;
		case 5:
			CutsceneProducer.Field.ChangeAreaTo(OriginArea);
			isVeritaTalking = true;
			CutsceneProducer.UIManager.FadeCanvas.FadeIn(1f, 1f).GetAwaiter().OnCompleted(delegate
			{
				CutsceneProducer.SoundManager.StopAMB();
				GetReward().Forget();
			});
			break;
		case 2:
		case 4:
			break;
		}
	}

	protected override void OnSkipTimeline(int state)
	{
		OnBeforeSkipTimeline();
		switch (state)
		{
		case 0:
			PlayerLookIdle();
			ChangeArea();
			break;
		case 1:
			ChangeArea();
			break;
		case 5:
			PlayerLookIdle();
			break;
		}
	}

	public override bool ShouldLoop(int index)
	{
		int num = base.CurrentState;
		if (num != 0)
		{
			if (num == 5 && (uint)index <= 1u)
			{
				goto IL_001a;
			}
		}
		else if ((uint)index <= 1u)
		{
			goto IL_001a;
		}
		return false;
		IL_001a:
		return CutsceneProducer.IsTalking;
	}

	public override void EnterLoop(int index)
	{
		switch (base.CurrentState)
		{
		default:
			return;
		case 0:
			switch (index)
			{
			default:
				return;
			case 0:
				StartScenarioInsideCutscene(index);
				return;
			case 1:
				break;
			}
			break;
		case 5:
			if ((uint)index > 1u)
			{
				return;
			}
			break;
		}
		StartScenarioInsideCutscene(index);
	}

	public override void ExitLoop(int index)
	{
		switch (base.CurrentState)
		{
		case 0:
			if (index != 0 && index == 1)
			{
				CutsceneProducer.SoundManager.StopBGM();
			}
			break;
		case 5:
			_ = 1;
			break;
		}
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

	public void MovePlayerEnding()
	{
		walkBackOffset = Mathf.Sign(Boss.transform.position.x - endingPlayerPosition.position.x) * playerWalkBackDistance;
		CutsceneProducer.Player.MoveXPositionAsync(endingPlayerPosition.position.x, isRunning: true).GetAwaiter().OnCompleted(delegate
		{
			FlipPlayerToFigure();
			BossCutsceneProducer.InvokeAfterWaitPlayerAnim(delegate
			{
				CutsceneProducer.Player.mover.SetAnimation("see_low");
			});
		});
	}

	public void PlayerCrouch(bool value)
	{
		CutsceneProducer.Player.mover.SetAnimation(value ? "crouch" : "crouch_end", loop: false);
		CutsceneProducer.Player.mover.AddAnimation(value ? "crouch_loop" : "cam_up");
	}

	public void FlipCorFigure()
	{
		corFigure.localScale = new Vector3(-1f, 1f, 1f);
	}

	public void FlipPlayerLeft()
	{
		CutsceneProducer.Player.transform.SynchronizePositionDirection(endingPlayerCenterPosition);
		CutsceneProducer.Player.mover.Flip(Direction.Left);
	}

	public void FadeOut()
	{
		CutsceneProducer.UIManager.FadeCanvas.FadeOut(1f).GetAwaiter().OnCompleted(delegate
		{
			endingFinishCam.gameObject.SetActive(value: false);
			PlayerLookIdle();
		});
	}

	public void ShiftMoldinBGM()
	{
		CutsceneProducer.SoundManager.ShiftMoldinBGM();
	}

	public void PlayerWalkBack()
	{
		Vector3 pos = CutsceneProducer.Player.transform.position;
		float current = 0f;
		DOTween.Sequence().Append(DOTween.To(() => current, delegate(float x)
		{
			current = x;
			CutsceneProducer.Player.transform.position = new Vector3(pos.x + current, pos.y, pos.z);
		}, 0f - walkBackOffset, 1.5833334f).SetEase(Ease.Linear)).PrependCallback(delegate
		{
			CutsceneProducer.Player.mover.SetAnimation("scene/back_away_2", loop: false);
			CutsceneProducer.Player.mover.AddAnimation("scene/back_away_2_loop");
		});
	}

	private void AdjustPositionsPhase()
	{
		FlipBossToPlayer();
		bossFigure.SynchronizePositionDirection(Boss.transform);
	}

	private void AdjustPositionsEnding()
	{
		FlipBossToTarget(base.FightArea.AreaBox.Center.x);
		float diffX = Boss.transform.position.x - endingBossPosition.position.x;
		float x = endingPlayerPosition.position.x;
		BossCutsceneProducer.AlignTransformXs(endingBossPositions, diffX, Boss.transform.localScale.x > 0f, Boss.transform.position.x);
		BossCutsceneProducer.AlignTransformXs(endingPlayerPositionsWithoutFlip, endingPlayerPosition.position.x - x, flipCondition: false);
	}

	private async UniTaskVoid GetReward()
	{
		new PureItem("MoldinSpirit").AddToPlayerData(ItemAcquireDisplayOrder.ShowFullPanel);
		await UniTask.WaitWhile(() => CutsceneProducer.FieldCanvas.InteractionPanel.IsDisplaying);
		CutsceneProducer.Player.PlayerStat.AddStatAppliedExperience(Boss.Exp);
		Game.Instance.Player.PlayerPart.OnKill();
		EarnMoney();
		await UniTask.WaitWhile(() => CutsceneProducer.FieldCanvas.LevelBoard.IsLevelUpProducing);
		new PureItem("RoyalSealPremium").AddToPlayerData(ItemAcquireDisplayOrder.ShowNotification);
		if (Game.Instance.SaveData.TalkData.GetInteractCount(ActorName.Verita) > 0)
		{
			ShowResonance(delegate
			{
				isVeritaTalking = false;
			});
		}
		else
		{
			isVeritaTalking = false;
		}
	}
}
