using System;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using Spine.Unity.Playables;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Cutscene;

public class VeritaGateCutsceneProducer : CutsceneProducer
{
	private enum State
	{
		Cutscene,
		VeritaCleared
	}

	[Header("Timeline Assets")]
	[SerializeField]
	private PlayableAsset gateTalkTimeline;

	[SerializeField]
	private PlayableAsset gateOpenTimeline;

	[Header("VCams")]
	[SerializeField]
	private CinemachineVirtualCamera gateTalkVCam;

	[SerializeField]
	private CinemachineVirtualCamera gateOpenVCam;

	[SerializeField]
	private CinemachineVirtualCamera gateOpenEndVCam;

	[Header("Transform")]
	[SerializeField]
	private Transform fakePlayer;

	[SerializeField]
	private Transform openGateTransform;

	[Header("References")]
	[SerializeField]
	private InteractTrigger interactTrigger;

	[SerializeField]
	private BoxCollider2D entranceCollider;

	private State cutsceneState;

	private CancellationToken cancellationToken;

	private bool isTriggerEnable;

	protected override bool SkipCondition
	{
		get
		{
			if (base.SkipCondition)
			{
				if (cutsceneState == State.Cutscene)
				{
					return base.PlayableDirector.time > 0.0;
				}
				return true;
			}
			return false;
		}
	}

	protected override void SetProducerState(int state)
	{
		cutsceneState = (State)state;
		State state2 = cutsceneState;
		if (state2 != State.Cutscene)
		{
			if (state2 == State.VeritaCleared)
			{
				if (!isTriggerEnable)
				{
					goto IL_0029;
				}
				if (isTriggerEnable)
				{
					goto IL_0066;
				}
			}
		}
		else
		{
			if (!isTriggerEnable)
			{
				goto IL_0029;
			}
			if (isTriggerEnable)
			{
				goto IL_0066;
			}
		}
		interactTrigger.gameObject.SetActive(value: false);
		entranceCollider.enabled = true;
		return;
		IL_0029:
		isTriggerEnable = true;
		interactTrigger.OnInteract += delegate
		{
			PlayTimelineAsync(1, blockControl: true).Forget();
		};
		entranceCollider.enabled = false;
		return;
		IL_0066:
		isTriggerEnable = true;
		entranceCollider.enabled = false;
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		cancellationToken = token;
		switch (cutsceneState)
		{
		case State.Cutscene:
			base.FieldController.ChangeAreaTo(base.MyArea);
			base.PlayableDirector.playableAsset = gateTalkTimeline;
			base.PlayableDirector.time = 0.0;
			base.PlayableDirector.Evaluate();
			SynchronizePlayer(fakePlayer);
			await UniTask.WaitUntil(() => CutsceneProducer.Player.mover.IsOnGround, PlayerLoopTiming.Update, token);
			CutsceneProducer.Player.mover.SetAnimationInstantly("cam_up");
			BindPlayerTracks(value: true);
			await CutsceneProducer.FadeCanvas.FadeIn(2f, 0f, Ease.Linear, token);
			break;
		case State.VeritaCleared:
			await CutsceneProducer.Player.MoveXPositionAsync(openGateTransform.position.x, isRunning: false, token);
			await BlendFieldCamToCutsceneStartCamAsync(gateOpenVCam);
			base.PlayableDirector.playableAsset = gateOpenTimeline;
			base.PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Spine Animation State Track", CutsceneProducer.Player.mover.SkeletonAnimation);
			break;
		}
	}

	protected override void OnEndTimeline(int state)
	{
		switch (cutsceneState)
		{
		case State.Cutscene:
			OnTalkEnd().Forget();
			break;
		case State.VeritaCleared:
			Game.Instance.LoadFieldAsync(SceneDataModel.MoveModel(FieldID.ConvertToFieldID("WorldsEnd_01"), "li_04 - we_01")).Forget();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	protected override void OnSkipTimeline(int state)
	{
		switch (cutsceneState)
		{
		case State.Cutscene:
			base.PlayableDirector.time = base.PlayableDirector.duration;
			base.PlayableDirector.Evaluate();
			break;
		case State.VeritaCleared:
			DisableFadeInAfterSkip();
			break;
		}
	}

	public override bool ShouldLoop(int index)
	{
		return cutsceneState switch
		{
			State.Cutscene => CutsceneProducer.IsTalking, 
			_ => false, 
		};
	}

	public override void EnterLoop(int index)
	{
		if (cutsceneState != State.Cutscene)
		{
			_ = 1;
		}
		else
		{
			StartScenarioInsideCutscene(index);
		}
	}

	public void WhiteOut(float seconds)
	{
		CutsceneProducer.FadeCanvas.FadeOut(seconds, 0f, cancellationToken).Forget();
	}

	public void FadeIn(float seconds)
	{
		CutsceneProducer.FadeCanvas.FadeIn(seconds, 0f, Ease.Linear, cancellationToken).Forget();
	}

	private async UniTaskVoid OnTalkEnd()
	{
		BindPlayerTracks(value: false);
		CutsceneProducer.Player.mover.SetAnimationInstantly("idle");
		CutsceneProducer.MemoryPanel.TryUpdateClearedMemory(wasCleared: false, ActorName.Verita);
		CutsceneProducer.MemoryPanel.TryUpdateClearedMemory(wasCleared: false, ActorName.Haechi);
		await UniTask.WaitWhile(() => base.CineBrain.IsBlending, PlayerLoopTiming.Update, cancellationToken);
		await CutsceneProducer.FadeCanvas.FadeIn(2f, 0f, Ease.Linear, cancellationToken);
	}
}
