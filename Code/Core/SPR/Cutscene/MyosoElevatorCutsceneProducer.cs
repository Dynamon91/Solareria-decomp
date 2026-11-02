using System;
using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;
using UnityEngine;

namespace SPR.Cutscene;

public class MyosoElevatorCutsceneProducer : CutsceneProducer
{
	[SerializeField]
	private CinemachineVirtualCamera startVcam;

	[SerializeField]
	private CinemachineVirtualCamera endVcam;

	[SerializeField]
	private Transform playerStartPosition;

	[SerializeField]
	private ElevatorBackgroundController elevatorBackgroundController;

	[SerializeField]
	private List<GameObject> elevatorDebris;

	private Action onElevatorStop;

	protected override void SetProducerState(int state)
	{
		elevatorDebris.ForEach(delegate(GameObject go)
		{
			go.SetActive(state == 1);
		});
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		await BlendFieldCamToCutsceneStartCamAsync(startVcam);
		UniTaskExtension.WaitForFixedUpdatesOrCancel(60, token).GetAwaiter().OnCompleted(delegate
		{
			playerStartPosition.position = CutsceneProducer.Player.transform.position;
			BindPlayerTracks(value: true, tween: true);
		});
	}

	protected override void OnEndTimeline(int state)
	{
		SynchronizeEndVcamAsync(endVcam).Forget();
		BindPlayerTracks(value: false, tween: true);
		CutsceneProducer.AddInteractCount(ActorName.Myoso);
		CutsceneProducer.Field.ChangeAreaTo(GetComponentInParent<Area>());
	}

	protected override void OnSkipTimeline(int state)
	{
		base.PlayableDirector.time = base.PlayableDirector.duration;
		base.PlayableDirector.Evaluate();
		elevatorBackgroundController.Stop();
		OnElevatorStop();
	}

	public override bool ShouldLoop(int index)
	{
		return CutsceneProducer.IsTalking;
	}

	public override void EnterLoop(int index)
	{
		StartScenarioInsideCutscene(index);
	}

	public void ChangeElevatorSpeed()
	{
		elevatorBackgroundController.ChangeSpeed(111f, 3f, 2.5f).Forget();
	}

	public void SetPlayerIdleState()
	{
		CutsceneProducer.Player.mover.PSM.ChangeState(CutsceneProducer.Player.mover.PSM.IdleState);
	}

	public void FlipPlayerRight()
	{
		CutsceneProducer.Player.mover.Flip(Direction.Right);
	}

	public void OnElevatorStop()
	{
		onElevatorStop();
	}

	public void SetPlayerIntroStart()
	{
		CutsceneProducer.Player.mover.SetAnimation("intro_start", loop: false);
	}

	public void SetActionOnElevatorStop(Action action)
	{
		onElevatorStop = action;
	}
}
