using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using Spine.Unity;
using Spine.Unity.Playables;
using UnityEngine;
using UnityEngine.Timeline;

namespace SPR.Cutscene;

public class NpcRescueCutsceneProducer : CutsceneProducer
{
	[Header("VCam")]
	[SerializeField]
	private CinemachineVirtualCamera vCam;

	[Header("Transform")]
	[SerializeField]
	private Transform fakePlayer;

	[SerializeField]
	private Transform startTransform;

	[Header("Reference")]
	[SerializeField]
	private SkeletonAnimation npcSkeletonAnimation;

	[SerializeField]
	private InteractTrigger interactTrigger;

	[SerializeField]
	private GameObject lunyaFigure;

	private bool LunyaEnable => Trigger.CheckTrigger("Trigger_InviteLunya");

	private void Awake()
	{
		interactTrigger.OnInteract += delegate
		{
			PlayTimelineAsync(0, blockControl: true).Forget();
		};
	}

	protected override void SetProducerState(int state)
	{
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		await CutsceneProducer.Player.MoveXPositionAsync(startTransform.position.x, isRunning: false, token);
		CutsceneProducer.Player.mover.Flip(Direction.Left);
		await BlendFieldCamToCutsceneStartCamAsync(vCam);
		base.PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Spine Animation State Track", CutsceneProducer.Player.mover.SkeletonAnimation);
		if (!LunyaEnable)
		{
			npcSkeletonAnimation.Skeleton.SetSkin("Lunya_None");
			lunyaFigure.SetActive(value: false);
			base.PlayableDirector.TrackBinding<ActivationTrack>("Lunya Activation Track", null);
			base.PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Lunya Spine Animation State Track", null);
		}
	}

	protected override void OnEndTimeline(int state)
	{
		SwitchNormalLibrary().Forget();
	}

	protected override void OnSkipTimeline(int state)
	{
		DisableFadeInAfterSkip();
	}

	private static async UniTaskVoid SwitchNormalLibrary()
	{
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(SectorName.Library, out var data))
		{
			data.SaveObjectState("Library Environment Switcher", 1);
		}
		await CutsceneProducer.FadeCanvas.FadeOut(1f);
		Game.Instance.LoadFieldAsync(SceneDataModel.MoveModel(new FieldID(SectorName.Library, 3), "Npc_Rescued")).Forget();
	}

	public override bool ShouldLoop(int index)
	{
		return CutsceneProducer.IsTalking;
	}

	public override void EnterLoop(int index)
	{
		StartScenarioInsideCutscene(index);
	}
}
