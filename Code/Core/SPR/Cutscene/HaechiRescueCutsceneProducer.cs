using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using Library;
using SPR.Field;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Timeline;

namespace SPR.Cutscene;

public class HaechiRescueCutsceneProducer : CutsceneProducer
{
	private const int DefaultState = 0;

	public const int UnlockedState = 1;

	private const int RescuedState = 2;

	private const int AfterRescuedState = 3;

	private const int ByeState = 4;

	private const int AfterByeState = 5;

	[Header("Timeline Assets")]
	[SerializeField]
	private TimelineAsset rescueTimeline;

	[SerializeField]
	private TimelineAsset wallTimeline;

	[SerializeField]
	private TimelineAsset byeTimeline;

	[Header("Rescue")]
	[SerializeField]
	private CinemachineVirtualCamera startVcam;

	[SerializeField]
	private CinemachineVirtualCamera endVcam;

	[SerializeField]
	private Transform playerBreakPosition;

	[SerializeField]
	private Transform playerPetPosition;

	[Header("Wall")]
	[SerializeField]
	private Transform haechiWallPosition;

	[SerializeField]
	private EventReference barkEvent;

	[Header("Bye")]
	[SerializeField]
	private Transform haechiRunPosition;

	[SerializeField]
	private Transform haechiFlyPosition;

	[SerializeField]
	private EventReference runEvent;

	[Header("Stuck Event")]
	[SerializeField]
	private EventReference stuckEvent;

	[SerializeField]
	private Transform stuckEventSource;

	[SerializeField]
	private List<AreaBox> stuckAreas;

	[Header("References")]
	[SerializeField]
	private Transform haechiFigure;

	[SerializeField]
	private DefenseLocker defenseLocker;

	[SerializeField]
	private BreakableWall breakableWall;

	[SerializeField]
	private CutsceneInteraction cutsceneInteraction;

	[SerializeField]
	private HaechiButtonAction buttonAction;

	[SerializeField]
	private List<GameObject> rockPileObjects;

	[SerializeField]
	private List<GameObject> backgroundObjects;

	private DefenseLocker defenseObject;

	private EventInstance stuckInstance;

	private EventInstance barkInstance;

	private EventInstance runInstance;

	private List<Vector2> originalVcamArea;

	private List<Vector2> unlockedVcamArea;

	private SkeletonAnimation haechiSpine;

	private const float BarkPeriod = 3.7666667f;

	private const float BarkOffset = 3.3333333f;

	private float barkTimer;

	private SkeletonAnimation HaechiSpine => haechiSpine ?? (haechiSpine = haechiFigure.GetComponentInChildren<SkeletonAnimation>());

	private PolygonCollider2D VcamBoundingShape => base.MyArea.ConfineCollider;

	private bool IsRescuedButNotBye
	{
		get
		{
			int num = base.CurrentState;
			return num == 2 || num == 3;
		}
	}

	private bool ByeAfterRescue
	{
		get
		{
			int num = base.CurrentState;
			return num == 4 || num == 5;
		}
	}

	private void OnDestroy()
	{
		if (runInstance.isValid())
		{
			runInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			runInstance.release();
		}
	}

	private void FixedUpdate()
	{
		if (Game.Instance.CurrentState != GameState.Initialized || IsProducing || (base.CurrentState == 2 && base.PlayableDirector.time > 0.0) || !haechiFigure.gameObject.activeSelf)
		{
			return;
		}
		if (base.CurrentState == 0)
		{
			CutsceneProducer.WorldData.TryGetSectorData(Game.Instance.CurrentFieldID.SectorName, out var data);
			if (data.TryGetObjectStateNum(defenseLocker.ID, out var state) && state == 1)
			{
				ChangeState(1);
				SetProducerState(1);
			}
		}
		else if (IsRescuedButNotBye)
		{
			CutsceneProducer.WorldData.TryGetSectorData(Game.Instance.CurrentFieldID.SectorName, out var data2);
			if (data2.TryGetObjectStateNum(breakableWall.ID, out var state2) && state2 == breakableWall.HitCount)
			{
				PlayTimelineAsync(4, blockControl: false, hideCanvas: false).Forget();
			}
			barkTimer += Time.deltaTime;
			if (barkTimer >= 3.3333333f && !barkInstance.isValid())
			{
				barkInstance = CutsceneProducer.SoundManager.PlayOneShotAttached(barkEvent, haechiFigure);
			}
			if (barkTimer >= 3.7666667f)
			{
				barkTimer -= 3.7666667f;
			}
		}
		if (base.CurrentState >= 2)
		{
			return;
		}
		if (!stuckInstance.isValid())
		{
			stuckInstance = CutsceneProducer.SoundManager.PlayOneShotAttached(stuckEvent, stuckEventSource);
		}
		if (stuckInstance.isValid())
		{
			int num = (stuckAreas.Any((AreaBox a) => a.IsInArea(CutsceneProducer.Player.mover.VisualCenter)) ? 1 : 0);
			stuckInstance.setVolume(num);
		}
	}

	protected override void SetProducerState(int state)
	{
		if (originalVcamArea.IsNullOrEmpty())
		{
			originalVcamArea = new List<Vector2>();
			VcamBoundingShape.GetPath(0, originalVcamArea);
		}
		if (unlockedVcamArea.IsNullOrEmpty())
		{
			unlockedVcamArea = new List<Vector2>();
			GetComponentInChildren<PolygonCollider2D>().GetPath(0, unlockedVcamArea);
		}
		cutsceneInteraction.gameObject.SetActive(state == 1);
		rockPileObjects.ForEach(delegate(GameObject go)
		{
			go.SetActive(state < 2);
		});
		backgroundObjects.ForEach(delegate(GameObject go)
		{
			go.SetActive(state >= 2);
		});
		switch (state)
		{
		case 0:
			HaechiSpine.SetAnimationInstantly("curl_shiver");
			VcamBoundingShape.SetPath(0, unlockedVcamArea);
			break;
		case 1:
			HaechiSpine.SetAnimationInstantly("curl_shiver");
			cutsceneInteraction.AwakeObject(Game.Instance.SaveData);
			cutsceneInteraction.gameObject.SetActive(value: true);
			cutsceneInteraction.SetInteractCallBack(delegate
			{
				CutsceneProducer.InputManager.BlockControlForTask(StartButtonActionAsync(), "Start Haechi Button Action").Forget();
			});
			VcamBoundingShape.SetPath(0, unlockedVcamArea);
			break;
		case 3:
		case 4:
		case 5:
			if (!IsProducing)
			{
				haechiFigure.gameObject.SetActive(value: false);
			}
			break;
		case 2:
			break;
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 1:
			base.PlayableDirector.playableAsset = rescueTimeline;
			BindPlayerTracks(value: true);
			ApplyAshBreakVelocity();
			startVcam.gameObject.SetActive(value: false);
			if (stuckInstance.isValid())
			{
				stuckInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
				stuckInstance.release();
			}
			break;
		case 2:
			base.PlayableDirector.playableAsset = wallTimeline;
			haechiFigure.Flip(-1f);
			break;
		case 4:
			base.PlayableDirector.playableAsset = byeTimeline;
			if (barkInstance.isValid())
			{
				barkInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
				barkInstance.release();
			}
			break;
		}
		await UniTask.CompletedTask;
	}

	protected override void OnEndTimeline(int state)
	{
		switch (state)
		{
		case 1:
			VcamBoundingShape.SetPath(0, originalVcamArea);
			base.MyArea.Vcam.GetComponent<CinemachineConfiner>().InvalidatePathCache();
			cutsceneInteraction.gameObject.SetActive(value: false);
			PlayRescueTimelineAsync().Forget();
			CutsceneProducer.MemoryPanel.UpdateMemory(ActorName.Haechi, isClear: false);
			CutsceneProducer.WanderingData.EnqueueResonanceAfterMoveScene("HaechiRescue_Resonance");
			break;
		case 2:
			haechiFigure.SynchronizePositionDirection(haechiWallPosition);
			break;
		case 4:
		{
			haechiFigure.Flip(1f);
			HaechiSpine.SetAnimationInstantly("run");
			runInstance = CutsceneProducer.SoundManager.PlayOneShotAttached(runEvent, haechiFigure);
			float duration = (haechiRunPosition.position.x - haechiWallPosition.position.x) / 40f;
			float duration2 = (haechiFlyPosition.position.x - haechiRunPosition.position.x) / 60f;
			float num = (float)base.FieldController.MainArea.AreaBox.UpperRight.x + 20f;
			float duration3 = (num - haechiFlyPosition.position.x) / 60f;
			DOTween.Sequence().Append(haechiFigure.DOMoveX(haechiRunPosition.position.x, duration).SetEase(Ease.Linear).OnComplete(delegate
			{
				HaechiSpine.SetAnimation("fly");
				runInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
				runInstance.release();
			})).Append(haechiFigure.DOMoveX(haechiFlyPosition.position.x, duration2).SetEase(Ease.Linear))
				.Join(haechiFigure.DOMoveY(haechiFlyPosition.position.y, duration2).SetEase(Ease.OutSine))
				.Append(haechiFigure.DOMoveX(num, duration3).SetEase(Ease.Linear))
				.AppendInterval(5f)
				.OnComplete(delegate
				{
					haechiFigure.gameObject.SetActive(value: false);
				});
			break;
		}
		case 3:
			break;
		}
	}

	private async UniTask PlayRescueTimelineAsync()
	{
		UniTask uniTask = SynchronizeEndVcamAsync(endVcam);
		UniTask uniTask2 = UniTask.WaitUntil(() => CutsceneProducer.FadeCanvas.IsWhite);
		await UniTask.WhenAll(uniTask, uniTask2);
		PlayTimelineAsync(2, blockControl: false, hideCanvas: false, checkIsProducing: false).Forget();
	}

	protected override void OnSkipTimeline(int state)
	{
		base.PlayableDirector.time = base.PlayableDirector.duration;
		base.PlayableDirector.Evaluate();
	}

	public void WalkPlayer()
	{
		CutsceneProducer.Player.MoveXPositionAsync(playerPetPosition.position.x, isRunning: false).Forget();
	}

	private async UniTask StartButtonActionAsync()
	{
		CutsceneProducer.UIManager.FieldCanvas.HideAllAsync(instant: false).Forget();
		await UniTask.WaitUntil(() => CutsceneProducer.Player.mover.IsOnGround);
		await CutsceneProducer.Player.MoveXPositionAsync(playerBreakPosition.position.x, isRunning: true);
		CutsceneProducer.Player.mover.Flip(Direction.Left);
		await BlendFieldCamToCutsceneStartCamAsync(startVcam);
		startVcam.gameObject.SetActive(value: true);
		CutsceneProducer.Player.mover.SetAnimation("scene/heachi_burst_charge_start", loop: false);
		await CutsceneProducer.Player.mover.SkeletonAnimation.WaitAnimationAsync();
		CutsceneProducer.Player.mover.AddAnimation("scene/heachi_burst_charge_loop");
		await CutsceneProducer.InputManager.StackControlTarget(buttonAction);
	}

	public void EscapeButtonAction()
	{
		CutsceneProducer.UIManager.FieldCanvas.ShowAllAsync(instant: false).Forget();
		CutsceneProducer.Player.mover.SetAnimation("idle");
		SynchronizeEndVcamAsync(startVcam).GetAwaiter().OnCompleted(delegate
		{
			cutsceneInteraction.gameObject.SetActive(value: true);
		});
	}

	private static void ApplyAshBreakVelocity()
	{
		PlayerStateMachine psm = CutsceneProducer.Player.mover.PSM;
		PlayerControlParameter instance = PlayerControlParameter.Instance;
		CutsceneProducer.Player.mover.ResetRelativeVelocity();
		psm.ChangeState(psm.CutsceneState);
		psm.SetStateOptionActive(StateOption.GravityFree, active: false);
		CutsceneProducer.Player.mover.ApplyRelativeVelocity(instance.AshBreakVelocity * new Vector2((!CutsceneProducer.Player.mover.IsFacingRight) ? 1 : (-1), 1f), faceJump: false);
		PlayerUniTaskUtil.WaitForFixedUpdatesOrCancel(instance.ashBreakPostdelayFrame).GetAwaiter().OnCompleted(delegate
		{
			psm.ChangeState(CutsceneProducer.Player.mover.IsOnGround ? psm.IdleState : psm.FallState);
			psm.SetStateOptionActive(StateOption.GravityFree, active: false);
		});
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
		foreach (AreaBox stuckArea in stuckAreas)
		{
			Gizmos.DrawCube(stuckArea.Center, stuckArea.Size.ToVector3());
		}
	}
}
