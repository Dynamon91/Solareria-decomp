using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Timeline;

namespace SPR.Cutscene;

public class ChasaRiverCutsceneProducer : CutsceneProducer
{
	[Header("Timeline")]
	[SerializeField]
	private TimelineAsset startTimeline;

	[SerializeField]
	private TimelineAsset boatTimeline;

	[SerializeField]
	private TimelineAsset arrivedTimeline;

	[Header("References")]
	[SerializeField]
	private GameObject chasaFigure;

	[SerializeField]
	private GameObject totFigure;

	[SerializeField]
	private GameObject boat;

	[SerializeField]
	private BoneFollower chasaBoneFollower;

	[SerializeField]
	private BoneFollower totBoneFollower;

	[SerializeField]
	private ParallaxLoopController parallaxLoopController;

	[SerializeField]
	private CutsceneInteraction cutsceneInteraction;

	[SerializeField]
	private CutsceneTrigger cutsceneTrigger;

	[Header("Vcams")]
	[SerializeField]
	private CinemachineVirtualCamera startFirstVcam;

	[SerializeField]
	private CinemachineVirtualCamera endLastVcam;

	[Header("Positions")]
	[SerializeField]
	private Transform startPlayerTransform;

	[SerializeField]
	private Transform startChasaTransform;

	[SerializeField]
	private Transform startBoatTransform;

	[SerializeField]
	private Transform endBoatTransform;

	[Header("Variables")]
	[SerializeField]
	private float fadeDuration;

	[SerializeField]
	private float backgroundMoveSpeed;

	private const int DefaultState = 0;

	private const int BoatState = 1;

	private const int ArrivedState = 2;

	private const int FinishedState = 3;

	private bool loopParallax;

	[Space]
	[SerializeField]
	private bool test;

	[SerializeField]
	private int testCutsceneIndex;

	private bool loadTest;

	protected override void SetProducerState(int state)
	{
		if (test && !loadTest)
		{
			loadTest = true;
			UniTask.WaitUntil(() => CutsceneProducer.InputManager.IsCurrentControlTarget(CutsceneProducer.Player)).GetAwaiter().OnCompleted(delegate
			{
				PlayTimelineAsync(testCutsceneIndex, blockControl: true).Forget();
			});
			return;
		}
		switch (state)
		{
		case 0:
			chasaFigure.transform.position = startChasaTransform.position;
			chasaFigure.SetActive(value: true);
			totFigure.SetActive(value: false);
			boat.SetActive(value: true);
			boat.transform.position = startBoatTransform.position;
			cutsceneInteraction.SetInteractCallBack(delegate
			{
				CutsceneProducer.InputManager.BlockControlForTask(PlayStartCutsceneAsync(), "River Cutscene Start").Forget();
			});
			cutsceneTrigger.OnTriggered += delegate
			{
				cutsceneInteraction.gameObject.SetActive(value: true);
			};
			cutsceneTrigger.gameObject.SetActive(value: true);
			break;
		case 3:
			chasaFigure.SetActive(value: false);
			totFigure.SetActive(value: false);
			boat.SetActive(value: true);
			boat.transform.position = endBoatTransform.position;
			break;
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 0:
			base.PlayableDirector.playableAsset = startTimeline;
			MoveCutsceneStartCamToCurrentCam(startFirstVcam);
			CutsceneProducer.SoundManager.PlayBGM("ChasaRiver");
			break;
		case 1:
			base.PlayableDirector.playableAsset = boatTimeline;
			base.PlayableDirector.Evaluate();
			chasaBoneFollower.enabled = true;
			chasaBoneFollower.followXYPosition = true;
			totBoneFollower.enabled = true;
			totBoneFollower.followXYPosition = true;
			await UniTask.WaitForSeconds(fadeDuration, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
			CutsceneProducer.FadeCanvas.FadeIn(1f).Forget();
			break;
		case 2:
			base.PlayableDirector.playableAsset = arrivedTimeline;
			await UniTask.WaitForSeconds(fadeDuration, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
			CutsceneProducer.FadeCanvas.FadeIn(1f).Forget();
			break;
		}
	}

	protected override void OnEndTimeline(int state)
	{
		switch (state)
		{
		case 0:
			PlayNextTimelineAsync(state).Forget();
			break;
		case 1:
			PlayNextTimelineAsync(state).Forget();
			chasaBoneFollower.enabled = false;
			totBoneFollower.enabled = false;
			totFigure.SetActive(value: false);
			break;
		case 2:
			SynchronizeEndVcamAsync(endLastVcam).Forget();
			CutsceneProducer.Player.mover.SynchronizeAnimation(totFigure.GetComponentInChildren<SkeletonAnimation>());
			SynchronizePlayer(totFigure.transform);
			totFigure.SetActive(value: false);
			CutsceneProducer.Player.mover.SetAnimation("idle");
			StopBGM();
			CutsceneProducer.UIManager.MenuCanvas.MemoryPanel.UpdateMemory(ActorName.Chasa, isClear: true);
			break;
		}
	}

	protected override void OnSkipTimeline(int state)
	{
		if (state < 2)
		{
			DisableFadeInAfterSkip();
		}
		else
		{
			chasaFigure.SetActive(value: false);
		}
	}

	private async UniTask PlayStartCutsceneAsync()
	{
		CutsceneProducer.FieldCanvas.HideAllAsync(instant: false).Forget();
		await UniTask.WaitUntil(() => CutsceneProducer.Player.mover.IsOnGround);
		await CutsceneProducer.Player.MoveXPositionAsync(startPlayerTransform.position.x, isRunning: false);
		CutsceneProducer.Player.mover.Flip(Direction.Right);
		PlayTimelineAsync(0, blockControl: true).Forget();
	}

	private async UniTask PlayNextTimelineAsync(int currentState)
	{
		await UniTask.NextFrame();
		PlayTimelineAsync(currentState + 1, blockControl: true).Forget();
	}

	public override void EnterLoop(int index)
	{
		StartScenarioInsideCutscene(index);
	}

	public override bool ShouldLoop(int index)
	{
		return CutsceneProducer.IsTalking;
	}

	public void FadeOut()
	{
		CutsceneProducer.FadeCanvas.FadeOut(1f).Forget();
	}

	public void SetParallaxLayerLoop(bool value)
	{
		if (value)
		{
			parallaxLoopController.Initialize();
		}
		loopParallax = value;
	}

	private void FixedUpdate()
	{
		if (loopParallax)
		{
			parallaxLoopController.Move(backgroundMoveSpeed);
		}
	}
}
