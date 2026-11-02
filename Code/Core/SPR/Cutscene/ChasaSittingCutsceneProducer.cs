using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace SPR.Cutscene;

public class ChasaSittingCutsceneProducer : CutsceneProducer
{
	private const int InitialState = 0;

	private const int TalkedState = 1;

	[SerializeField]
	private CinemachineVirtualCamera startVcam;

	[SerializeField]
	private CinemachineVirtualCamera endVcam;

	[SerializeField]
	private Chasa chasaNpc;

	[SerializeField]
	private List<GameObject> cutsceneObjects;

	private Vector2 playerPos;

	protected override bool SkipCondition
	{
		get
		{
			if (base.SkipCondition)
			{
				return base.PlayableDirector.time > 0.0;
			}
			return false;
		}
	}

	protected override void SetProducerState(int state)
	{
		cutsceneObjects.ForEach(delegate(GameObject go)
		{
			go.SetActive(value: false);
		});
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		await CutsceneProducer.FadeCanvas.FadeOut(0.5f, 0f, token);
		playerPos = CutsceneProducer.Player.transform.position;
		CutsceneProducer.Player.SetPosition(Util.DisablePosition);
		chasaNpc.gameObject.SetActive(value: false);
		cutsceneObjects.ForEach(delegate(GameObject go)
		{
			go.SetActive(value: true);
		});
		BlendFieldCamToCutsceneStartCamAsync(startVcam, 0.1f).Forget();
		CutsceneProducer.SoundManager.PlayBGM("ChasaCastle");
		await UniTask.WaitForSeconds(0.5f, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		CutsceneProducer.FadeCanvas.FadeIn(0.5f, 0f, Ease.Linear, token).Forget();
	}

	protected override void OnEndTimeline(int state)
	{
		SustainVcamForFade(endVcam);
		CutsceneProducer.InputManager.BlockControlForTask(EndTimelineAsync(), "ChasaSitting End Fade").Forget();
	}

	protected override void OnSkipTimeline(int state)
	{
		DisableFadeInAfterSkip();
		base.PlayableDirector.time = base.PlayableDirector.duration;
		base.PlayableDirector.Evaluate();
		chasaNpc.DisableTalkVcam();
		CutsceneProducer.SoundManager.FadeOutBGM(1f, allowFadeOut: false);
	}

	public override bool ShouldLoop(int index)
	{
		return CutsceneProducer.IsTalking;
	}

	public override void EnterLoop(int index)
	{
		StartScenarioInsideCutscene(index);
	}

	public override void ExitLoop(int index)
	{
		if (index == 4)
		{
			CutsceneProducer.SoundManager.StopBGM();
		}
	}

	private async UniTask EndTimelineAsync()
	{
		await CutsceneProducer.FadeCanvas.FadeOut(1f);
		SetProducerState(1);
		CutsceneProducer.Player.SetPosition(playerPos, isFacingRight: false);
		CutsceneProducer.Player.mover.SetAnimation("idle");
		SynchronizeEndVcamAsync(endVcam).Forget();
		await UniTask.WaitForSeconds(1f);
		CutsceneProducer.UIManager.FadeCanvas.FadeIn(0.5f).Forget();
	}
}
