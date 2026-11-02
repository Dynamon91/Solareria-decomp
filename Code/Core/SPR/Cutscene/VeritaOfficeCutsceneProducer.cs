using System;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using Spine.Unity.Playables;
using UnityEngine;

namespace SPR.Cutscene;

public class VeritaOfficeCutsceneProducer : CutsceneProducer
{
	private enum State
	{
		None,
		Playing,
		Watched
	}

	[Header("VCams")]
	[SerializeField]
	private CinemachineVirtualCamera startVCam;

	[SerializeField]
	private CinemachineVirtualCamera endVCam;

	[Header("References")]
	[SerializeField]
	private CutsceneTrigger trigger;

	protected override void SetProducerState(int state)
	{
		switch ((State)state)
		{
		case State.None:
			trigger.OnTriggered += delegate
			{
				if (!Trigger.CheckTrigger("Trigger_VeritaInfected"))
				{
					PlayTimelineAsync(1, blockControl: true).Forget();
				}
			};
			break;
		case State.Watched:
			trigger.gameObject.SetActive(value: false);
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		case State.Playing:
			break;
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch ((State)state)
		{
		case State.Playing:
			await BlendFieldCamToCutsceneStartCamAsync(startVCam, 1f);
			base.PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Player Spine Animation State Track", CutsceneProducer.Player.mover.SkeletonAnimation);
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		case State.None:
		case State.Watched:
			break;
		}
		await UniTask.CompletedTask;
	}

	protected override void OnEndTimeline(int state)
	{
		switch ((State)state)
		{
		case State.Playing:
			CutsceneProducer.InputManager.BlockControlForTask(EndTimelineAsync(), "Verita Office End Fade").Forget();
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		case State.None:
		case State.Watched:
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
		case State.Playing:
		case State.Watched:
			return;
		}
		throw new ArgumentOutOfRangeException("state", state, null);
	}

	public override bool ShouldLoop(int index)
	{
		switch ((State)base.CurrentState)
		{
		case State.Playing:
			return CutsceneProducer.IsTalking;
		default:
			throw new ArgumentOutOfRangeException();
		case State.None:
		case State.Watched:
			return false;
		}
	}

	public override void EnterLoop(int index)
	{
		switch ((State)base.CurrentState)
		{
		case State.Playing:
			StartScenarioInsideCutscene(index);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case State.None:
		case State.Watched:
			break;
		}
	}

	private async UniTask EndTimelineAsync()
	{
		await CutsceneProducer.UIManager.FadeCanvas.FadeOut(1f);
		endVCam.gameObject.SetActive(value: false);
		await UniTask.WaitForSeconds(1);
		CutsceneProducer.UIManager.FadeCanvas.FadeIn(1f).Forget();
	}
}
