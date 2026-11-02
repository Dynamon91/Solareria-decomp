using System;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;
using SPR.Library;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Cutscene;

public class FiveWarriorsCutscene : CutsceneProducer
{
	private enum State
	{
		None,
		Gaia,
		Soowoo,
		Baran,
		Moldin,
		Chasa,
		PrimordialFlame,
		EnterBossScene,
		EntranceOpened
	}

	[Header("Timeline")]
	[SerializeField]
	private PlayableAsset gaiaPlayableAsset;

	[SerializeField]
	private PlayableAsset soowooPlayableAsset;

	[SerializeField]
	private PlayableAsset baranPlayableAsset;

	[SerializeField]
	private PlayableAsset moldinPlayableAsset;

	[SerializeField]
	private PlayableAsset chasaPlayableAsset;

	[SerializeField]
	private PlayableAsset primordialFlamePlayableAsset;

	[Header("Other Timeline")]
	[SerializeField]
	private PlayableAsset enterBossScenePlayableAsset;

	[Header("Vcam")]
	[SerializeField]
	private CinemachineVirtualCamera primordialFlameStartVcam;

	[SerializeField]
	private CinemachineVirtualCamera primordialFlameEndVcam;

	[SerializeField]
	private CinemachineVirtualCamera enterBossSceneVCam;

	[SerializeField]
	private float statueCutsceneCompleteOrthoSize = 25f;

	[Header("Transform")]
	[SerializeField]
	private Transform fakePlayer;

	[SerializeField]
	private Transform gaiaTransform;

	[SerializeField]
	private Transform soowooTransform;

	[SerializeField]
	private Transform baranTransform;

	[SerializeField]
	private Transform moldinTransform;

	[SerializeField]
	private Transform chasaTransform;

	[SerializeField]
	private Transform primordialFlameStartTransform;

	[SerializeField]
	private Transform primordialFlameEndTransform;

	[Header("Animatior")]
	[SerializeField]
	private Animator gaiaStatueAnimator;

	[SerializeField]
	private Animator soowooStatueAnimator;

	[SerializeField]
	private Animator baranStatueAnimator;

	[SerializeField]
	private Animator moldinStatueAnimator;

	[SerializeField]
	private Animator chasaStatueAnimator;

	[SerializeField]
	private Animator primordialFlameAnimator;

	[Header("Area Changer")]
	[SerializeField]
	private AreaChanger[] areaChangers;

	[Header("Reference")]
	[SerializeField]
	private WorldsEndStatue gaiaStatue;

	[SerializeField]
	private WorldsEndStatue soowooStatue;

	[SerializeField]
	private WorldsEndStatue baranStatue;

	[SerializeField]
	private WorldsEndStatue moldinStatue;

	[SerializeField]
	private WorldsEndStatue chasaStatue;

	[SerializeField]
	private WorldsEndStatue primordialFlameStatue;

	[SerializeField]
	private GameObject[] transparentWalls;

	[SerializeField]
	private GameObject[] fogs;

	[SerializeField]
	private SkeletonAnimation[] gates;

	[SerializeField]
	private SubArea primordialFlameStatueArea;

	[SerializeField]
	private CutsceneTrigger primordialFlameTrigger;

	[SerializeField]
	private OtherEntrance bossEntrance;

	private void Start()
	{
		if (!gaiaStatue.EventSubscribed)
		{
			gaiaStatue.OnInteractAction += delegate
			{
				PlayTimelineAsync(1, blockControl: true).Forget();
			};
		}
		if (!soowooStatue.EventSubscribed)
		{
			soowooStatue.OnInteractAction += delegate
			{
				PlayTimelineAsync(2, blockControl: true).Forget();
			};
		}
		if (!baranStatue.EventSubscribed)
		{
			baranStatue.OnInteractAction += delegate
			{
				PlayTimelineAsync(3, blockControl: true).Forget();
			};
		}
		if (!moldinStatue.EventSubscribed)
		{
			moldinStatue.OnInteractAction += delegate
			{
				PlayTimelineAsync(4, blockControl: true).Forget();
			};
		}
		if (!chasaStatue.EventSubscribed)
		{
			chasaStatue.OnInteractAction += delegate
			{
				PlayTimelineAsync(5, blockControl: true).Forget();
			};
		}
	}

	protected override void SetProducerState(int state)
	{
		fakePlayer.gameObject.SetActive(value: false);
		OpenWall(Mathf.Max(0, state - 1), Game.Instance.CurrentState != GameState.Initialized);
		if (!primordialFlameTrigger.EventSubscribed && state <= 6)
		{
			primordialFlameTrigger.OnTriggered += delegate
			{
				PlayTimelineAsync(6, blockControl: true).Forget();
			};
		}
		if (!primordialFlameStatue.EventSubscribed)
		{
			primordialFlameStatue.OnInteractAction += ((state < 8) ? ((Action)delegate
			{
				PlayTimelineAsync(7, blockControl: true).Forget();
				primordialFlameStatue.ClearAction();
			}) : ((Action)delegate
			{
				PlayTimelineAsync(8, blockControl: true).Forget();
			}));
		}
		SetStatueState((State)state);
		for (int num = 0; num < Mathf.Clamp(state - 1, 0, areaChangers.Length - 1); num++)
		{
			areaChangers[num].gameObject.SetActive(value: false);
		}
		switch ((State)state)
		{
		case State.None:
			base.PlayableDirector.playableAsset = null;
			break;
		case State.Gaia:
			base.PlayableDirector.playableAsset = gaiaPlayableAsset;
			break;
		case State.Soowoo:
			base.PlayableDirector.playableAsset = soowooPlayableAsset;
			break;
		case State.Baran:
			base.PlayableDirector.playableAsset = baranPlayableAsset;
			break;
		case State.Moldin:
			base.PlayableDirector.playableAsset = moldinPlayableAsset;
			break;
		case State.Chasa:
			base.PlayableDirector.playableAsset = chasaPlayableAsset;
			break;
		case State.PrimordialFlame:
			base.PlayableDirector.playableAsset = primordialFlamePlayableAsset;
			break;
		case State.EnterBossScene:
			base.PlayableDirector.playableAsset = enterBossScenePlayableAsset;
			primordialFlameStatueArea.Vcam.m_Lens.OrthographicSize = statueCutsceneCompleteOrthoSize;
			primordialFlameStatueArea.GetComponent<AutoCameraZoom>().SetRightSize(statueCutsceneCompleteOrthoSize);
			break;
		default:
			primordialFlameStatueArea.Vcam.m_Lens.OrthographicSize = statueCutsceneCompleteOrthoSize;
			primordialFlameStatueArea.GetComponent<AutoCameraZoom>().SetRightSize(statueCutsceneCompleteOrthoSize);
			break;
		}
		if ((object)base.PlayableDirector.playableAsset != null)
		{
			BindPlayerTracks(value: true);
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		areaChangers[Mathf.Min(state - 1, areaChangers.Length - 1)].gameObject.SetActive(value: false);
		switch ((State)state)
		{
		case State.Gaia:
			await CutsceneProducer.Player.MoveXPositionAsync(gaiaTransform.position.x, isRunning: false, token);
			CutsceneProducer.Player.transform.SynchronizePositionDirection(gaiaTransform);
			break;
		case State.Soowoo:
			await CutsceneProducer.Player.MoveXPositionAsync(soowooTransform.position.x, isRunning: false, token);
			CutsceneProducer.Player.transform.SynchronizePositionDirection(soowooTransform);
			break;
		case State.Baran:
			await CutsceneProducer.Player.MoveXPositionAsync(baranTransform.position.x, isRunning: false, token);
			CutsceneProducer.Player.transform.SynchronizePositionDirection(baranTransform);
			break;
		case State.Moldin:
			await CutsceneProducer.Player.MoveXPositionAsync(moldinTransform.position.x, isRunning: false, token);
			CutsceneProducer.Player.transform.SynchronizePositionDirection(moldinTransform);
			break;
		case State.Chasa:
			await CutsceneProducer.Player.MoveXPositionAsync(chasaTransform.position.x, isRunning: false, token);
			CutsceneProducer.Player.transform.SynchronizePositionDirection(chasaTransform);
			break;
		case State.PrimordialFlame:
			await CutsceneProducer.Player.MoveXPositionAsync(primordialFlameStartTransform.position.x, isRunning: false, token);
			CutsceneProducer.Player.transform.SynchronizePositionDirection(primordialFlameStartTransform);
			await BlendFieldCamToCutsceneStartCamAsync(primordialFlameStartVcam, 1f);
			primordialFlameEndVcam.Priority = primordialFlameStartVcam.Priority + 1;
			break;
		case State.EnterBossScene:
			await CutsceneProducer.Player.MoveXPositionAsync(primordialFlameStartTransform.position.x, isRunning: false, token);
			await BlendFieldCamToCutsceneStartCamAsync(enterBossSceneVCam, 1f);
			break;
		default:
			await CutsceneProducer.Player.MoveXPositionAsync(primordialFlameStartTransform.position.x, isRunning: false, token);
			await BlendFieldCamToCutsceneStartCamAsync(enterBossSceneVCam, 1f);
			await UniTask.WaitForSeconds(0.1f, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
			CutsceneProducer.Player.mover.SetAnimation("turn_back", loop: false);
			CutsceneProducer.Player.mover.AddAnimation("idle_back");
			await UniTask.WaitForSeconds(2f, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
			EndTimeline();
			break;
		case State.None:
			break;
		}
		await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
	}

	protected override void OnEndTimeline(int state)
	{
		OpenWall(state, setGateOpen: true);
		switch ((State)state)
		{
		case State.Gaia:
			base.FieldController.ChangeAreaTo(base.FieldController.MainArea);
			gaiaStatue.ChangeState(2);
			CutsceneProducer.Player.mover.SetAnimation("idle");
			break;
		case State.Soowoo:
			base.FieldController.ChangeAreaTo(base.FieldController.MainArea);
			soowooStatue.ChangeState(2);
			CutsceneProducer.Player.mover.SetAnimation("idle");
			break;
		case State.Baran:
			base.FieldController.ChangeAreaTo(base.FieldController.MainArea);
			baranStatue.ChangeState(2);
			CutsceneProducer.Player.mover.SetAnimation("idle");
			break;
		case State.Moldin:
			base.FieldController.ChangeAreaTo(base.FieldController.MainArea);
			moldinStatue.ChangeState(2);
			CutsceneProducer.Player.mover.SetAnimation("idle");
			break;
		case State.Chasa:
			base.FieldController.ChangeAreaTo(base.FieldController.MainArea);
			chasaStatue.ChangeState(2);
			CutsceneProducer.Player.mover.SetAnimation("idle");
			break;
		case State.PrimordialFlame:
			CutsceneProducer.Player.transform.SynchronizePositionDirection(primordialFlameEndTransform);
			if ((CinemachineVirtualCamera)base.CineBrain.ActiveVirtualCamera == primordialFlameEndVcam)
			{
				SynchronizeEndVcamAsync(primordialFlameEndVcam).Forget();
			}
			SynchronizePlayer(primordialFlameEndTransform);
			if (!primordialFlameStatue.EventSubscribed)
			{
				primordialFlameStatue.OnInteractAction += delegate
				{
					PlayTimelineAsync(7, blockControl: true).Forget();
				};
			}
			primordialFlameStatueArea.Vcam.m_Lens.OrthographicSize = statueCutsceneCompleteOrthoSize;
			primordialFlameStatueArea.GetComponent<AutoCameraZoom>().SetRightSize(statueCutsceneCompleteOrthoSize);
			CutsceneProducer.Player.mover.SetAnimation("idle");
			break;
		case State.EnterBossScene:
		case State.EntranceOpened:
			bossEntrance.Interact();
			break;
		case State.None:
			break;
		}
	}

	protected override void OnSkipTimeline(int state)
	{
		base.PlayableDirector.time = base.PlayableDirector.duration;
		base.PlayableDirector.Evaluate();
		switch ((State)state)
		{
		case State.None:
		case State.Gaia:
		case State.Soowoo:
		case State.Baran:
		case State.Moldin:
		case State.Chasa:
		case State.PrimordialFlame:
			ChangeBgmParameter();
			break;
		case State.EnterBossScene:
			DisableFadeInAfterSkip();
			ChangeBgmParameter();
			CutsceneProducer.FadeCanvas.FadeOutWhiteInstantly();
			break;
		default:
			DisableFadeInAfterSkip();
			ChangeBgmParameter();
			break;
		}
	}

	public void FadeOut(float duration)
	{
		CutsceneProducer.FadeCanvas.FadeOut(duration).Forget();
	}

	public void FadeIn(float duration)
	{
		CutsceneProducer.FadeCanvas.FadeIn(duration).Forget();
	}

	private void OpenWall(int opened, bool setGateOpen)
	{
		opened = Mathf.Min(opened, transparentWalls.Length);
		for (int i = 0; i < opened; i++)
		{
			transparentWalls[i].SetActive(value: false);
			if (setGateOpen)
			{
				gates[i].AnimationState.SetAnimation(0, "end", loop: false);
				fogs[i].SetActive(value: false);
			}
		}
	}

	private void SetStatueState(State state)
	{
		switch (state)
		{
		case State.Soowoo:
			base.PlayableDirector.playableAsset = gaiaPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			break;
		case State.Baran:
			base.PlayableDirector.playableAsset = gaiaPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.playableAsset = soowooPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			break;
		case State.Moldin:
			base.PlayableDirector.playableAsset = gaiaPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.playableAsset = soowooPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.playableAsset = baranPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			break;
		case State.Chasa:
			base.PlayableDirector.playableAsset = gaiaPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.playableAsset = soowooPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.playableAsset = baranPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.playableAsset = moldinPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			break;
		case State.PrimordialFlame:
			base.PlayableDirector.playableAsset = gaiaPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.playableAsset = soowooPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.playableAsset = baranPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.playableAsset = moldinPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.playableAsset = chasaPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			break;
		default:
			base.PlayableDirector.playableAsset = gaiaPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.playableAsset = soowooPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.playableAsset = baranPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.playableAsset = moldinPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.playableAsset = chasaPlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.playableAsset = primordialFlamePlayableAsset;
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			base.PlayableDirector.playableAsset = null;
			break;
		case State.None:
		case State.Gaia:
			break;
		}
	}

	public void ChangeBgmParameter()
	{
		CutsceneProducer.WanderingData.SetWorldsEndBgmParameter(base.CurrentState);
		CutsceneProducer.SoundManager.SetWorldsEndBgmParameter();
	}

	public void WhiteFadeOut(float duration)
	{
		CutsceneProducer.FadeCanvas.FadeWhiteOut(duration).Forget();
	}

	private void AdjustCameraPosition()
	{
		Vector3 position = GraphicHelper.MainCamera.transform.position;
		GraphicHelper.MainCamera.transform.position = new Vector3(primordialFlameStatue.transform.position.x, position.y, position.z);
	}
}
