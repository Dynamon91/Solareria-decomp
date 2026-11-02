using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;
using Spine.Unity.Playables;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Cutscene;

public class ImahenBossCutsceneProducer : BossCutsceneProducer
{
	public enum State
	{
		Dying = -1,
		None,
		Start,
		AfterStart,
		Clear
	}

	[Header("Playable Assets")]
	[SerializeField]
	private PlayableAsset openingTimeline;

	[SerializeField]
	private PlayableAsset endTimeline;

	[Header("VCam")]
	[SerializeField]
	private CinemachineVirtualCamera openingStartVCam;

	[SerializeField]
	private CinemachineVirtualCamera openingEndVCam;

	[SerializeField]
	private CinemachineVirtualCamera endingStartVCam;

	[SerializeField]
	private CinemachineVirtualCamera endingEndVCam;

	[Header("References")]
	[SerializeField]
	private GameObject imahenHologramFigure;

	[SerializeField]
	private Transform playerOpeningTransform;

	[SerializeField]
	private CutsceneTrigger enterTrigger;

	[SerializeField]
	private GameObject[] transparentWalls;

	[SerializeField]
	private GameObject bossDoor;

	[SerializeField]
	private GameObject brokenDoor;

	[SerializeField]
	private Animator machineAnimator;

	[SerializeField]
	private Animator hologramAnimator;

	[SerializeField]
	private ParticleSystem hologramEffect;

	[SerializeField]
	private ImahenSawLever sawLever;

	[SerializeField]
	private GameObject brokenLever;

	[SerializeField]
	private GameObject machineSfxEmitter;

	[SerializeField]
	private ImahenChaser imahenChaser;

	protected override void SetProducerState(int state)
	{
		switch ((State)state)
		{
		case State.None:
		case State.AfterStart:
			enterTrigger.OnTriggered += delegate
			{
				PlayTimelineAsync(1, blockControl: true, hideCanvas: false).Forget();
			};
			break;
		default:
			SetObjectState();
			break;
		case State.Start:
			break;
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch ((State)state)
		{
		case State.Start:
			base.PlayableDirector.playableAsset = openingTimeline;
			await BlendFieldCamToCutsceneStartCamAsync(openingStartVCam, 1f);
			CutsceneProducer.Player.MoveXPositionAsync(playerOpeningTransform.position.x, isRunning: true, token).Forget();
			base.PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Spine Animation State Track", CutsceneProducer.Player.mover.SkeletonAnimation);
			LazySingleton<MessageSystem>.Instance.Publish(new ImahenChaserUpdateEvent(ImahenChaser.Mode.Stop));
			CutsceneProducer.FieldCanvas.RedAlertBoard.SetAlert(value: false);
			break;
		case State.Clear:
		{
			ClearFight(updateMemory: false, stopBgm: false);
			Boss.gameObject.SetActive(value: true);
			brokenDoor.SetActive(value: true);
			base.PlayableDirector.playableAsset = endTimeline;
			await BlendFieldCamToCutsceneStartCamAsync(endingStartVCam, 0.1f);
			LazySingleton<MessageSystem>.Instance.Publish(new ImahenChaserUpdateEvent(ImahenChaser.Mode.Stop));
			GameObject[] array = transparentWalls;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.SetActive(value: false);
			}
			machineSfxEmitter.SetActive(value: false);
			break;
		}
		}
	}

	protected override void OnEndTimeline(int state)
	{
		switch ((State)state)
		{
		case State.Start:
		{
			SynchronizeEndVcamAsync(openingEndVCam).Forget();
			LazySingleton<MessageSystem>.Instance.Publish(new ImahenChaserUpdateEvent(ImahenChaser.Mode.Boss, base.MyArea.AreaBox.LowerLeft.ToVector3()));
			Boss.ExitCutsceneState(Boss.transform);
			base.MyArea.CastObject<ImahenArea>().Activate();
			GameObject[] array = transparentWalls;
			for (int num = 0; num < array.Length; num++)
			{
				array[num].gameObject.SetActive(value: true);
			}
			machineAnimator.Play("Imahen Core Machine_Loop");
			hologramAnimator.Play("Imahen_Hologram_NoiseLoop");
			StartFight();
			break;
		}
		case State.Clear:
			SynchronizeEndVcamAsync(endingEndVCam).Forget();
			CutsceneProducer.ExecuteWithoutChangeBGM(delegate
			{
				base.FieldController.ChangeAreaTo(base.FieldController.MainArea);
			});
			LazySingleton<MessageSystem>.Instance.Publish(new ImahenChaserUpdateEvent(ImahenChaser.Mode.BossEnd, base.MyArea.AreaBox.UpperRight.ToVector3()));
			bossDoor.gameObject.SetActive(value: false);
			Boss.SetAnimationInstantly("stage_4/destruction_static");
			imahenChaser.Rumbling().Forget();
			break;
		}
	}

	protected override void OnSkipTimeline(int state)
	{
		switch ((State)state)
		{
		case State.Start:
			imahenHologramFigure.SetActive(value: true);
			hologramEffect.gameObject.SetActive(value: true);
			hologramEffect.Play();
			ChangeArea();
			PlayIdleSfx();
			break;
		case State.Clear:
			SetObjectState();
			hologramEffect.gameObject.SetActive(value: false);
			Boss.CastObject<ImahenCoreMachine>().TurnOffLight();
			break;
		}
	}

	private void SetObjectState()
	{
		imahenHologramFigure.SetActive(value: false);
		sawLever.gameObject.SetActive(value: false);
		brokenLever.gameObject.SetActive(value: true);
		bossDoor.gameObject.SetActive(value: false);
		brokenDoor.SetActive(value: true);
	}

	public void ReturnChaser()
	{
		LazySingleton<MessageSystem>.Instance.Publish(new ImahenChaserUpdateEvent(ImahenChaser.Mode.Return));
	}

	public void RedAlert(bool value)
	{
		CutsceneProducer.FieldCanvas.RedAlertBoard.SetAlert(value);
	}

	public void PlayIdleSfx()
	{
		machineSfxEmitter.SetActive(value: true);
	}
}
