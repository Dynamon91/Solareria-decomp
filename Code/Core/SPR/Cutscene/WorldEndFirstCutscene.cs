using System;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Spine.Unity;
using UnityEngine;

namespace SPR.Cutscene;

public class WorldEndFirstCutscene : CutsceneProducer
{
	private enum State
	{
		None,
		Playing,
		Watched
	}

	[SerializeField]
	private Transform totFigure;

	[SerializeField]
	private Transform endPositionTransform;

	[SerializeField]
	private CinemachineVirtualCamera zoom;

	protected override void SetProducerState(int state)
	{
		switch ((State)state)
		{
		case State.None:
			WaitForPlayerControl().Forget();
			break;
		default:
			base.gameObject.SetActive(value: false);
			break;
		case State.Playing:
			break;
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch ((State)state)
		{
		case State.Playing:
			CutsceneProducer.Player.mover.SetAnimationInstantly("idle");
			SwitchToFakeSpine(CutsceneProducer.Player.mover, totFigure, totFigure.GetComponentInChildren<SkeletonAnimation>());
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
			SynchronizePlayer(totFigure, Vector3.up);
			SynchronizeEndVcamAsync(zoom).Forget();
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
		if ((uint)state <= 2u)
		{
			totFigure.position = endPositionTransform.position;
			return;
		}
		throw new ArgumentOutOfRangeException("state", state, null);
	}

	private async UniTaskVoid WaitForPlayerControl()
	{
		CutsceneProducer.Player.mover.PSM.ChangeState(CutsceneProducer.Player.mover.PSM.IdleState);
		await UniTask.WaitUntil(() => CutsceneProducer.InputManager.IsCurrentControlTarget(CutsceneProducer.Player));
		PlayTimelineAsync(1, blockControl: true).Forget();
	}
}
