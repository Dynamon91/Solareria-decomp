using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace SPR.Cutscene;

public class ImahenLabCutsceneProducer : CutsceneProducer
{
	[SerializeField]
	private Transform imahenFigure;

	[SerializeField]
	private CinemachineVirtualCamera startVcam;

	[SerializeField]
	private CinemachineVirtualCamera endVcam;

	[SerializeField]
	private Transform playerPosition;

	[SerializeField]
	private MorphedImahen morphedImahenObject;

	[SerializeField]
	private CutsceneTrigger cutsceneTrigger;

	private SkeletonAnimation imahenSpine;

	private static Camera MainCamera => Camera.main;

	private SkeletonAnimation ImahenSpine => imahenSpine ?? (imahenSpine = imahenFigure.GetComponentInChildren<SkeletonAnimation>());

	protected override void SetProducerState(int state)
	{
		bool flag = state > 0;
		imahenFigure.gameObject.SetActive(!flag);
		morphedImahenObject.gameObject.SetActive(flag);
		if (flag)
		{
			return;
		}
		cutsceneTrigger.OnTriggered += delegate
		{
			UniTask.WaitUntil(() => Game.Instance.CurrentState == GameState.Initialized).GetAwaiter().OnCompleted(delegate
			{
				PlayTimelineAsync(0, blockControl: true).Forget();
			});
		};
		morphedImahenObject.SetActiveIdleSfx(value: true);
	}

	protected override UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		MoveCutsceneStartCamToCurrentCam(startVcam);
		MovePlayerAndLookUpAsync().Forget();
		CutsceneProducer.SoundManager.StopBGM();
		return default(UniTask);
	}

	protected override void OnEndTimeline(int state)
	{
		PlayerLookIdle();
		SynchronizeEndVcamAsync(endVcam).Forget();
		morphedImahenObject.gameObject.SetActive(value: true);
		morphedImahenObject.Spine.SynchronizeAnimation(ImahenSpine);
		morphedImahenObject.Spine.Skeleton.UpdateWorldTransform(Skeleton.Physics.Reset);
		imahenFigure.gameObject.SetActive(value: false);
		CutsceneProducer.MemoryPanel.UpdateMemory(ActorName.Imahen, isClear: true);
		CutsceneProducer.SoundManager.PlayBGM();
	}

	protected override void OnSkipTimeline(int state)
	{
		base.PlayableDirector.time = base.PlayableDirector.duration;
		base.PlayableDirector.Evaluate();
		SetCameraPlatformCullingMask(value: true);
		SetActivePlayer(value: true);
		CutsceneProducer.Player.mover.SetAnimation("idle");
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
	}

	public void SetCameraPlatformCullingMask(bool value)
	{
		if (value)
		{
			MainCamera.cullingMask |= LayerUtility.PlatformLayer;
		}
		else
		{
			MainCamera.cullingMask &= ~(int)LayerUtility.PlatformLayer;
		}
	}

	public void SetActivePlayer(bool value)
	{
		CutsceneProducer.Player.SetPosition(value ? ((Vector2)playerPosition.position) : Util.DisablePosition);
		if (value)
		{
			PlayerLookUp();
		}
	}

	public void SetSkin(string value)
	{
		ImahenSpine.Skeleton.SetSkin(value);
		ImahenSpine.Skeleton.SetSlotsToSetupPose();
		ImahenSpine.AnimationState.Apply(ImahenSpine.Skeleton);
	}

	public void SetActiveMorphedImahenSfx(bool value)
	{
		morphedImahenObject.SetActiveIdleSfx(value);
	}

	private async UniTask MovePlayerAndLookUpAsync()
	{
		await CutsceneProducer.Player.MoveXPositionAsync(playerPosition.position.x, isRunning: true);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(5);
		PlayerLookUp();
	}
}
