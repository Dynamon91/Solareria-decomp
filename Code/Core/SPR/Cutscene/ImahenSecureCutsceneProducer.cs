using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;
using Spine.Unity;
using UnityEngine;

namespace SPR.Cutscene;

public class ImahenSecureCutsceneProducer : CutsceneProducer
{
	[SerializeField]
	private CinemachineVirtualCamera vcam;

	[SerializeField]
	private Fog fog;

	[SerializeField]
	private SkeletonAnimation doorSpine;

	[SerializeField]
	private Transform playerPosition;

	[SerializeField]
	private GameObject transparentDoor;

	[SerializeField]
	private CutsceneInteraction cutsceneInteraction;

	[SerializeField]
	private GameObject healSfxEmitter;

	private BoneFollower[] boneFollowers;

	private bool? targetState;

	protected override void SetProducerState(int state)
	{
		bool flag = state > 0;
		doorSpine.SetAnimation(flag ? "open_loop" : "static");
		transparentDoor.SetActive(!flag);
		cutsceneInteraction.gameObject.SetActive(value: false);
		if (!flag)
		{
			fog.FadeOutAsync(instant: true).Forget();
			healSfxEmitter.SetActive(value: false);
			cutsceneInteraction.AwakeObject(Game.Instance.SaveData);
			cutsceneInteraction.gameObject.SetActive(targetState ?? true);
			cutsceneInteraction.SetInteractCallBack(delegate
			{
				PlayTimelineAsync(0, blockControl: true).Forget();
			});
		}
		else
		{
			fog.FadeInAsync(instant: true).Forget();
			healSfxEmitter.SetActive(value: true);
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		await CutsceneProducer.Player.MoveXPositionAsync(playerPosition.position.x, isRunning: false, token);
		CutsceneProducer.Player.mover.Flip(Direction.Right);
		await BlendFieldCamToCutsceneStartCamAsync(vcam);
		BindPlayerTracks(value: true);
		FollowBone(CutsceneProducer.Player.mover.SkeletonRenderer);
	}

	protected override void OnEndTimeline(int state)
	{
		SynchronizeEndVcamAsync(vcam).Forget();
		BindPlayerTracks(value: false);
		transparentDoor.SetActive(value: false);
		cutsceneInteraction.gameObject.SetActive(value: false);
	}

	protected override void OnSkipTimeline(int state)
	{
		base.PlayableDirector.time = base.PlayableDirector.playableAsset.duration;
		base.PlayableDirector.Evaluate();
		RevealFog();
	}

	public void RevealFog()
	{
		fog.FadeInAsync(instant: false).Forget();
		healSfxEmitter.SetActive(value: true);
	}

	private void FollowBone(SkeletonRenderer targetSpine)
	{
		if (boneFollowers == null)
		{
			boneFollowers = GetComponentsInChildren<BoneFollower>(includeInactive: true);
		}
		BoneFollower[] array = boneFollowers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].skeletonRenderer = targetSpine;
		}
	}

	public void SetInteractionActive(bool value)
	{
		targetState = value;
		cutsceneInteraction.gameObject.SetActive(value);
		DebugLog.GeneralLog($"SetInteractionActive = {targetState}");
	}
}
