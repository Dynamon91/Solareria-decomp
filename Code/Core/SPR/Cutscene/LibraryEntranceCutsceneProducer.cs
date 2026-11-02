using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR.Cutscene;

public class LibraryEntranceCutsceneProducer : CutsceneProducer
{
	[SerializeField]
	private CinemachineVirtualCamera startVcam;

	[SerializeField]
	private CinemachineVirtualCamera endVcam;

	protected override void SetProducerState(int state)
	{
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		await CutsceneProducer.Player.MoveXPositionAsync(startVcam.transform.position.x, isRunning: false, token);
		await BlendFieldCamToCutsceneStartCamAsync(startVcam);
	}

	protected override void OnEndTimeline(int state)
	{
		SustainVcamForFade(endVcam);
		CutsceneProducer.NotShowFieldCanvasUntilSceneChange();
	}

	protected override void OnSkipTimeline(int state)
	{
		DisableFadeInAfterSkip();
	}

	public void PlayerTurnBack()
	{
		CutsceneProducer.Player.mover.SetAnimation("turn_back_2", loop: false);
		CutsceneProducer.Player.mover.AddAnimation("idle_back_2");
	}

	public void PlayerWalkBack()
	{
		CutsceneProducer.Player.mover.SetAnimation("walk_turn_back");
	}

	public void PlayBGMChoir()
	{
		CutsceneProducer.SoundManager.SetAshHillsBGMChoir(value: true);
		UniTask.WaitForSeconds(15).GetAwaiter().OnCompleted(delegate
		{
			CutsceneProducer.SoundManager.SetAshHillsBGMChoir(value: false);
		});
	}
}
