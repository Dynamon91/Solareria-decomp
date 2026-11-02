using System;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LapinLibrary;
using Library;
using SPR.UI.Map;
using UnityEngine;
using UnityEngine.Timeline;

namespace SPR.Cutscene;

public class ImahenDivisionCutsceneProducer : CutsceneProducer
{
	[Header("Timelines")]
	[SerializeField]
	private TimelineAsset whiteOutTimeline;

	[SerializeField]
	private TimelineAsset flashbackTimeline;

	[SerializeField]
	private TimelineAsset hologramTimeline;

	[Header("Variables")]
	[SerializeField]
	private float fadeDelay = 1f;

	[SerializeField]
	private float alertDuration = 1f;

	[SerializeField]
	private int alertEndPositionX;

	[Header("References")]
	[SerializeField]
	private GameObject areaChanger;

	[SerializeField]
	private Collider2D rightLeverCollider;

	[SerializeField]
	private CutsceneInteraction cutsceneInteraction;

	[SerializeField]
	private GameObject hologramObject;

	[SerializeField]
	private Transform totCenterTransform;

	[SerializeField]
	private Transform interactionTransform;

	[SerializeField]
	private CinemachineVirtualCamera flashbackEndVcam;

	[SerializeField]
	private CinemachineVirtualCamera hologramStartVcam;

	private const int DefaultState = 0;

	private const int EnterState = 1;

	private const int FlashbackState = 2;

	private const int AfterFlashbackState = 3;

	private const int InteractState = 4;

	private const int CompleteState = 5;

	private CancellationToken producingToken;

	private CancellationTokenSource producingTokenSource;

	private CancellationTokenSource alertTokenSource;

	private float PlayerEndPosX => areaChanger.transform.position.x + 5f;

	protected override void SetProducerState(int state)
	{
		switch (state)
		{
		case 0:
			SetInteractionCallBack();
			rightLeverCollider.enabled = false;
			break;
		case 1:
			areaChanger.SetActive(value: false);
			break;
		case 2:
		case 3:
			cutsceneInteraction.AwakeObject(Game.Instance.SaveData);
			cutsceneInteraction.gameObject.SetActive(value: true);
			SetInteractionCallBack();
			hologramObject.GetComponent<CutsceneInteraction>().AwakeObject(Game.Instance.SaveData);
			hologramObject.SetActive(value: true);
			rightLeverCollider.enabled = false;
			break;
		case 5:
			rightLeverCollider.enabled = true;
			break;
		case 4:
			break;
		}
		void SetInteractionCallBack()
		{
			cutsceneInteraction.SetInteractCallBack(delegate
			{
				CutsceneProducer.InputManager.BlockControlForTask(PlayInteractCutsceneAsync(), "Move To Interact").GetAwaiter().OnCompleted(delegate
				{
					PlayTimelineAsync(4, blockControl: true).Forget();
				});
			});
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch (state)
		{
		case 1:
			base.PlayableDirector.playableAsset = whiteOutTimeline;
			CutsceneProducer.Player.MoveXPositionAsync(areaChanger.transform.position.x + 5f, isRunning: true, token).Forget();
			break;
		case 2:
			base.PlayableDirector.playableAsset = flashbackTimeline;
			CutsceneProducer.FadeCanvas.FadeIn(1f, 0f, Ease.Linear, token).Forget();
			CutsceneProducer.Player.SetPosition(Util.DisablePosition);
			cutsceneInteraction.gameObject.SetActive(value: false);
			hologramObject.SetActive(value: false);
			break;
		case 4:
			await BlendFieldCamToCutsceneStartCamAsync(hologramStartVcam, 1f);
			base.PlayableDirector.playableAsset = hologramTimeline;
			break;
		case 3:
			break;
		}
	}

	protected override void OnEndTimeline(int state)
	{
		switch (state)
		{
		case 1:
			areaChanger.SetActive(value: true);
			if (IsSkipping)
			{
				break;
			}
			StopBGM();
			CutsceneProducer.InputManager.BlockControlForTask(DelayForFadeAsync(delegate
			{
				if (!producingToken.IsCancellationRequested)
				{
					PlayTimelineAsync(2, blockControl: true).Forget();
				}
			}), "Fade Delay").Forget();
			break;
		case 2:
			SynchronizeEndVcamAsync(flashbackEndVcam).Forget();
			CutsceneProducer.Field.ChangeAreaTo(base.MyArea);
			cutsceneInteraction.gameObject.SetActive(value: true);
			hologramObject.SetActive(value: true);
			if (IsSkipping)
			{
				break;
			}
			CutsceneProducer.InputManager.BlockControlForTask(DelayForFadeAsync(delegate
			{
				if (!producingToken.IsCancellationRequested)
				{
					CutsceneProducer.FadeCanvas.FadeIn(1f, 0f, Ease.Linear, producingToken).Forget();
				}
			}), "Fade Delay").Forget();
			break;
		case 4:
			rightLeverCollider.enabled = true;
			SynchronizeEndVcamAsync(hologramStartVcam).Forget();
			PlayAlertAsync().Forget();
			MenuMapPanel.AddInfoStoneInformation("InformationVeritaWarning");
			break;
		case 3:
			break;
		}
	}

	protected override void OnSkipTimeline(int state)
	{
		if (state != 4)
		{
			producingTokenSource?.Cancel();
			CutsceneProducer.Field.ChangeAreaTo(base.MyArea);
			SynchronizeEndVcamAsync(flashbackEndVcam).Forget();
			cutsceneInteraction.gameObject.SetActive(value: true);
			hologramObject.SetActive(value: true);
			CutsceneProducer.Player.SetPosition(totCenterTransform.position);
		}
	}

	public void PlayEnterCutscene()
	{
		producingToken = TaskUtil.RefreshToken(ref producingTokenSource);
		PlayTimelineAsync(1, blockControl: true).Forget();
	}

	private async UniTask DelayForFadeAsync(Action onCompleted)
	{
		await UniTask.WaitForSeconds(fadeDelay);
		if (!producingToken.IsCancellationRequested)
		{
			onCompleted?.Invoke();
		}
	}

	private async UniTask PlayInteractCutsceneAsync()
	{
		CutsceneProducer.FieldCanvas.HideAllAsync(instant: false).Forget();
		await UniTask.WaitUntil(() => CutsceneProducer.Player.mover.IsOnGround);
		await CutsceneProducer.Player.MoveXPositionAsync(interactionTransform.position.x, isRunning: false);
		CutsceneProducer.Player.mover.Flip(Direction.Right);
	}

	private async UniTask PlayAlertAsync()
	{
		await UniTask.Yield();
		CutsceneProducer.FieldCanvas.RedAlertBoard.SetAlert(value: true);
		string key = "Cutscene_ImahenLab_Warning";
		int index = 0;
		CancellationToken token = TaskUtil.RefreshToken(ref alertTokenSource);
		while (true)
		{
			if (CutsceneProducer.Player.mover.VisualCenter.x > (float)alertEndPositionX)
			{
				alertTokenSource?.Cancel();
			}
			if (token.IsCancellationRequested)
			{
				break;
			}
			CutsceneProducer.TalkCanvas.ShowSpecialTalk(key, index + 1, 2.5f, 1f);
			await UniTaskExtension.WaitForSeconds(3f, token);
			index ^= 1;
		}
		EndAlert();
	}

	private static void EndAlert()
	{
		CutsceneProducer.TalkCanvas.HideSpecialTalk();
		CutsceneProducer.FieldCanvas.RedAlertBoard.SetAlert(value: false);
	}

	public override void EnterLoop(int index)
	{
		StartScenarioInsideCutscene(index);
	}

	public override bool ShouldLoop(int index)
	{
		return CutsceneProducer.IsTalking;
	}

	public void FadeWhiteOut()
	{
		CutsceneProducer.FadeCanvas.FadeWhiteOut(1f).Forget();
	}

	public void FadeIn()
	{
		CutsceneProducer.FadeCanvas.FadeIn(1f).Forget();
	}

	public void SetPlayerAfterFlashback()
	{
		CutsceneProducer.Player.SetPosition(totCenterTransform.position);
		CutsceneProducer.Player.mover.SetAnimation("idle_back");
	}

	private void OnDisable()
	{
		alertTokenSource?.Cancel();
	}

	private void OnDestroy()
	{
		alertTokenSource?.Cancel();
		EndAlert();
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(1f, 0.9f, 0f, 0.3f);
		Gizmos.DrawCube(new Vector3(alertEndPositionX, -4f), new Vector3(1f, 20f));
	}
}
