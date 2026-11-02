using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LapinLibrary;
using Library;
using SPR.Field;
using Spine.Unity;
using Spine.Unity.Playables;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;

namespace SPR.Cutscene;

public class CharonCutsceneProducer : BossCutsceneProducer
{
	[Header("Timeline Assets")]
	[SerializeField]
	private PlayableAsset readyTimeline;

	[SerializeField]
	private PlayableAsset openingTimeline;

	[SerializeField]
	private PlayableAsset endingTimeline;

	[SerializeField]
	private PlayableAsset dyingTimeline;

	[Header("Opening")]
	[SerializeField]
	private CinemachineVirtualCamera openingStartVcam;

	[SerializeField]
	private CinemachineVirtualCamera openingEndVcam;

	[SerializeField]
	private Transform floorTransform;

	[Header("Ending")]
	[SerializeField]
	private float firstMoveX;

	[SerializeField]
	private float secondMoveX;

	[SerializeField]
	private float thirdMoveX;

	[SerializeField]
	private float jumpWaitSeconds;

	[Header("Dying")]
	[SerializeField]
	private Transform charonShoulderTransform;

	[SerializeField]
	private CinemachineVirtualCamera dyingEndVcam;

	[SerializeField]
	private Transform playerFinalPosition;

	[Header("References")]
	[SerializeField]
	private List<GameObject> transparentWalls;

	[SerializeField]
	private List<GameObject> charonBodyColliders;

	[SerializeField]
	private List<GameObject> charonBodyTransparentWalls;

	[SerializeField]
	private List<SkeletonPartsRenderer> parts;

	[SerializeField]
	private RideableBone rideOnBoneFollower;

	[SerializeField]
	private RideableBone rideOffBoneFollower;

	[SerializeField]
	private GameObject charonPlatform;

	[SerializeField]
	private CharonInteractableHand interactableHand;

	[SerializeField]
	private CutsceneTrigger retryTrigger;

	[SerializeField]
	private InteractTrigger interactTrigger;

	[SerializeField]
	private Transform charonCallTransform;

	[Header("Reward")]
	[Tooltip("카론 무너지는 컷신 이후 시체 돌다리 위에 생성되는 아이템")]
	[SerializeField]
	private string corpseDropItemName;

	[SerializeField]
	private Vector2Int corpseItemDropPos;

	private Charon charon;

	private bool shouldWaitForRideOn;

	private float playerLightInitialIntensity = -1f;

	private const int CallState = 0;

	private const int EncounterState = 1;

	private const int RetryState = 2;

	public const int EndState = 3;

	private const int DyingState = 4;

	private const int ClearState = 5;

	private const string DefaultSortingLayer = "Default";

	private const string PlatformSortingLayer = "Platform";

	private CancellationTokenSource playerMoveAsyncTokenSource;

	private Sequence handGlowSequence;

	private bool waitForGetItem;

	private bool isCharonClearProducing;

	public override bool IsProducing
	{
		get
		{
			if (!base.IsProducing)
			{
				return isCharonClearProducing;
			}
			return true;
		}
	}

	private Charon Charon => charon ?? (charon = base.FightArea.GetComponentInChildren<Charon>(includeInactive: true));

	protected override void SetProducerState(int state)
	{
		charonPlatform.SetActive(value: false);
		bossFigure.gameObject.SetActive(value: false);
		transparentWalls.ForEach(delegate(GameObject wall)
		{
			wall.SetActive(value: false);
		});
		switch (state)
		{
		case 0:
			if (interactTrigger.gameObject.activeSelf)
			{
				interactTrigger.OnInteract += delegate
				{
					Interact().Forget();
				};
				retryTrigger.gameObject.SetActive(value: false);
			}
			break;
		case 2:
			interactTrigger.gameObject.SetActive(value: false);
			retryTrigger.OnTriggered += delegate
			{
				PlayTimelineAsync(1, blockControl: true).Forget();
			};
			break;
		case 3:
			shouldWaitForRideOn = true;
			rideOnBoneFollower.gameObject.SetActive(value: false);
			rideOffBoneFollower.gameObject.SetActive(value: false);
			break;
		case 4:
			rideOnBoneFollower.gameObject.SetActive(value: false);
			rideOffBoneFollower.gameObject.SetActive(value: true);
			break;
		case 5:
			charonPlatform.SetActive(value: true);
			interactTrigger.gameObject.SetActive(value: false);
			retryTrigger.gameObject.SetActive(value: false);
			break;
		case 1:
			break;
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 0:
			base.PlayableDirector.playableAsset = readyTimeline;
			base.PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Spine Animation State Track (1)", CutsceneProducer.Player.mover.SkeletonAnimation);
			await BlendFieldCamToCutsceneStartCamAsync(openingStartVcam);
			openingStartVcam.gameObject.SetActive(value: true);
			break;
		case 1:
			base.PlayableDirector.playableAsset = openingTimeline;
			openingStartVcam.gameObject.SetActive(value: false);
			await BlendFieldCamToCutsceneStartCamAsync(openingStartVcam);
			bossFigure.gameObject.SetActive(value: true);
			charonBodyColliders.ForEach(delegate(GameObject coll)
			{
				coll.SetActive(value: false);
			});
			charonBodyTransparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: false);
			});
			break;
		case 3:
			base.PlayableDirector.playableAsset = endingTimeline;
			isCharonClearProducing = true;
			ClearFight();
			bossFigure.gameObject.SetActive(value: true);
			charonBodyColliders.ForEach(delegate(GameObject coll)
			{
				coll.SetActive(value: false);
			});
			charonBodyTransparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: false);
			});
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: true);
			});
			break;
		case 4:
			bossFigure.gameObject.SetActive(value: true);
			charonBodyColliders.ForEach(delegate(GameObject coll)
			{
				coll.SetActive(value: false);
			});
			CutsceneProducer.Field.ChangeAreaTo(OriginArea);
			base.PlayableDirector.Stop();
			base.PlayableDirector.playableAsset = dyingTimeline;
			base.PlayableDirector.Evaluate();
			CutsceneProducer.Player.SetPosition(charonShoulderTransform.position);
			CutsceneProducer.Player.mover.SetRideBone(rideOffBoneFollower);
			CutsceneProducer.UIManager.FadeCanvas.FadeIn(1f).Forget();
			break;
		case 2:
			break;
		}
	}

	protected override void OnEndTimeline(int state)
	{
		switch (state)
		{
		case 0:
		case 1:
		case 2:
			interactTrigger.gameObject.SetActive(value: false);
			openingStartVcam.gameObject.SetActive(value: false);
			SynchronizeEndVcamAsync(openingEndVcam).Forget();
			bossFigure.gameObject.SetActive(value: false);
			Charon.ExitCutsceneState(bossFigure);
			StartFight();
			transparentWalls.ForEach(delegate(GameObject wall)
			{
				wall.SetActive(value: true);
			});
			break;
		case 3:
		{
			Game.Instance.RuntimeData.SetBGMLessEvent(value: true);
			UniTask task = PlayDyingTimelineAsync();
			CutsceneProducer.InputManager.BlockControlForTask(task, "CharonTimelineEndToDying").Forget();
			break;
		}
		case 4:
		{
			isCharonClearProducing = false;
			SynchronizeEndVcamAsync(dyingEndVcam).Forget();
			charonPlatform.SetActive(value: true);
			bossFigure.gameObject.SetActive(value: false);
			CutsceneProducer.Player.mover.SetAnimation("idle");
			Vector2Int spawnPos = ((Vector2)base.transform.position + corpseItemDropPos.ToVector2()).ToVector2Int();
			ItemObject.SpawnItemAsync(corpseDropItemName, spawnPos).Forget();
			Game.Instance.RuntimeData.SetBGMLessEvent(value: false);
			EarnMoney();
			break;
		}
		}
	}

	protected override void OnSkipTimeline(int state)
	{
		OnBeforeSkipTimeline();
		switch (state)
		{
		case 0:
			CutsceneProducer.InputManager.RemoveControlTarget(this).Forget();
			ChangeArea();
			PlayerLookBack(value: false);
			ChangeState(1);
			break;
		case 1:
		case 2:
			CutsceneProducer.InputManager.RemoveControlTarget(this).Forget();
			ChangeArea();
			PlayerLookBack(value: false);
			break;
		case 4:
			CutsceneProducer.Player.mover.SetRideBone(null);
			playerMoveAsyncTokenSource?.Cancel();
			CutsceneProducer.Player.SetPosition(playerFinalPosition.position, isFacingRight: false);
			CutsceneProducer.Player.mover.PSM.ChangeState(CutsceneProducer.Player.mover.PSM.IdleState);
			break;
		case 3:
			break;
		}
	}

	private async UniTask PlayDyingTimelineAsync()
	{
		await CutsceneProducer.UIManager.FadeCanvas.FadeOut(1f);
		ChangeState(5);
		PlayTimelineAsync(4, blockControl: true).Forget();
	}

	private async UniTaskVoid PlayerAutoRideOnAsync()
	{
		NullControllable nullControllable = new NullControllable("Charon Cutscene");
		await CutsceneProducer.InputManager.StackControlTarget(nullControllable);
		await CutsceneProducer.Player.MoveXPositionAsync(firstMoveX, isRunning: true, TaskUtil.RefreshToken(ref playerMoveAsyncTokenSource));
		CutsceneProducer.Player.jump = true;
		await CutsceneProducer.Player.MoveXPositionAsync(secondMoveX, isRunning: true, TaskUtil.RefreshToken(ref playerMoveAsyncTokenSource));
		CutsceneProducer.Player.jump = false;
		await UniTask.WaitForSeconds(jumpWaitSeconds);
		CutsceneProducer.Player.jump = true;
		await CutsceneProducer.Player.MoveXPositionAsync(thirdMoveX, isRunning: true, TaskUtil.RefreshToken(ref playerMoveAsyncTokenSource));
		await CutsceneProducer.InputManager.RemoveControlTarget(nullControllable);
		shouldWaitForRideOn = false;
		interactableHand.DisableAll();
	}

	private async UniTask PlayerAutoRideOffAsync()
	{
		await CutsceneProducer.Player.MoveXPositionAsync(214f, isRunning: true, TaskUtil.RefreshToken(ref playerMoveAsyncTokenSource));
		CutsceneProducer.Player.jump = true;
		await CutsceneProducer.Player.MoveXPositionAsync(244f, isRunning: true, TaskUtil.RefreshToken(ref playerMoveAsyncTokenSource));
	}

	private async UniTaskVoid Interact()
	{
		await CutsceneProducer.InputManager.BlockControlForTask(CutsceneProducer.Player.MoveXPositionAsync(charonCallTransform.position.x, isRunning: false), "Calling Charon");
		CutsceneProducer.Player.mover.LookAt(interactTrigger.transform.position);
		await PlayTimelineAsync(0, blockControl: true);
	}

	public void ChangePartsLayer(string layerName)
	{
		parts.ForEach(delegate(SkeletonPartsRenderer p)
		{
			p.MeshRenderer.sortingLayerID = SortingLayer.NameToID(layerName);
		});
	}

	public override bool ShouldLoop(int index)
	{
		switch (base.CurrentState)
		{
		case 3:
			if (index == 1)
			{
				return shouldWaitForRideOn;
			}
			return false;
		case 4:
			if (CutsceneProducer.Player.mover.IsOnGround)
			{
				return !(CutsceneProducer.Player.mover.VisualCenter.y < -14.5f);
			}
			return true;
		default:
			return false;
		}
	}

	public override void EnterLoop(int index)
	{
		switch (base.CurrentState)
		{
		case 3:
			if (index == 1)
			{
				charonBodyColliders[0].SetActive(value: true);
				charonBodyColliders[1].SetActive(value: false);
				charonBodyColliders[2].SetActive(value: false);
				charonBodyColliders[3].SetActive(value: false);
				charonBodyTransparentWalls.ForEach(delegate(GameObject wall)
				{
					wall.SetActive(value: false);
				});
			}
			break;
		case 4:
			charonBodyTransparentWalls[3].SetActive(value: false);
			charonBodyColliders[3].SetActive(value: true);
			PlayerAutoRideOffAsync().Forget();
			break;
		}
	}

	public override void ExitLoop(int index)
	{
		switch (base.CurrentState)
		{
		case 3:
			if (index != 0)
			{
				CutsceneProducer.Player.mover.SetRideBone(rideOnBoneFollower);
				charonBodyTransparentWalls.ForEach(delegate(GameObject wall)
				{
					wall.SetActive(value: true);
				});
				transparentWalls.ForEach(delegate(GameObject wall)
				{
					wall.SetActive(value: false);
				});
				interactableHand.DisableAll();
			}
			break;
		case 4:
			CutsceneProducer.Player.mover.SetRideBone(null);
			rideOffBoneFollower.gameObject.SetActive(value: false);
			ChangePartsLayer("Default");
			CutsceneProducer.InputManager.StackControlTarget(this).Forget();
			CutsceneProducer.Player.MoveXPositionAsync(238f, isRunning: false, TaskUtil.RefreshToken(ref playerMoveAsyncTokenSource)).Forget();
			SetActivePlayerLight(value: true);
			break;
		}
	}

	public void SetActivePlayerLight(bool value)
	{
		Light2D componentInChildren = CutsceneProducer.Player.GetComponentInChildren<Light2D>();
		if (playerLightInitialIntensity < 0f)
		{
			playerLightInitialIntensity = componentInChildren.intensity;
		}
		componentInChildren.DoLightIntensity(value ? playerLightInitialIntensity : 0f, 0.5f);
	}

	public void FinishWaitAndMove()
	{
		shouldWaitForRideOn = false;
	}

	public void SetPlayerAnimation(string animName)
	{
		CutsceneProducer.Player.mover.SetAnimation(animName);
	}

	public void EnableHandInteraction()
	{
		DebugLog.GeneralLog("Enable Charon Hand Interaction");
		interactableHand.Enable();
		interactableHand.OnInteract += delegate
		{
			PlayerAutoRideOnAsync().Forget();
		};
	}

	public void PlayEncounterTimeline()
	{
		PlayTimelineAsync(1, blockControl: true).Forget();
	}

	private void OnDrawGizmosSelected()
	{
		Vector2 vector = (Vector2)base.transform.position + corpseItemDropPos.ToVector2();
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(vector, 1f);
	}
}
