using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Timeline;

namespace SPR.Cutscene;

public class HaechiFlyCutsceneProducer : CutsceneProducer
{
	private const int InitialState = 0;

	private const int AfterCutsceneState = 1;

	[SerializeField]
	private Transform playerFigure;

	[SerializeField]
	private CinemachineVirtualCamera startVcam;

	[SerializeField]
	private CinemachineVirtualCamera endVcam;

	[SerializeField]
	private Transform playerStartPosition;

	[SerializeField]
	private CutsceneTrigger cutsceneTrigger;

	[SerializeField]
	private Renderer hornLRenderer;

	[SerializeField]
	private BoneFollower boneFollower;

	[SerializeField]
	private ParticleSystem head1;

	[SerializeField]
	private ParticleSystem head2;

	[SerializeField]
	private AshHillsHorizontalElevator elevator;

	protected override void SetProducerState(int state)
	{
		playerFigure.gameObject.SetActive(value: false);
		if (state == 1)
		{
			return;
		}
		if (!Game.Instance.SaveData.WanderingData.IsWatchedCutscene("HaechiRescueCutsceneProducer", 1))
		{
			elevator.Deactivate();
			return;
		}
		cutsceneTrigger.OnTriggered += delegate
		{
			PlayTimelineAsync(0, blockControl: true).Forget();
		};
		if (state == 0)
		{
			elevator.Deactivate();
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		await UniTask.WaitUntil(() => CutsceneProducer.Player.mover.IsOnGround, PlayerLoopTiming.Update, token);
		await CutsceneProducer.Player.MoveXPositionAsync(playerStartPosition.position.x, isRunning: false, token);
		MoveCutsceneStartCamToCurrentCam(startVcam);
		CutsceneProducer.Player.mover.Flip(Direction.Right);
	}

	protected override void OnEndTimeline(int state)
	{
		SustainVcamForFade(endVcam);
		CutsceneProducer.NotShowFieldCanvasUntilSceneChange();
		CutsceneProducer.UIManager.FadeCanvas.FadeOut(1f).GetAwaiter().OnCompleted(delegate
		{
			Game.Instance.LoadFieldAsync(SceneDataModel.MoveCutsceneModel(new FieldID(SectorName.WindshadeSpire, 1), "HaechiLand")).Forget();
		});
		elevator.ChangeState(1);
	}

	protected override void OnSkipTimeline(int state)
	{
		DisableFadeInAfterSkip();
	}

	public void ChangePlayerToFigure()
	{
		playerFigure.SynchronizePositionDirection(CutsceneProducer.Player.transform);
		CutsceneProducer.Player.SetPosition(Util.DisablePosition);
		playerFigure.gameObject.SetActive(value: true);
		boneFollower.followXYPosition = false;
	}

	public void FlipPlayerFigure(bool isRight)
	{
		playerFigure.Flip(isRight ? 1 : (-1));
	}

	public void RideBone()
	{
		ParticleSystem.EmissionModule emission1 = head1.emission;
		ParticleSystem.EmissionModule emission2 = head2.emission;
		emission1.enabled = false;
		emission2.enabled = false;
		base.PlayableDirector.TrackBinding<AnimationTrack>("Player Animation Track", null);
		playerFigure.transform.localPosition = Vector2.up;
		boneFollower.enabled = true;
		boneFollower.followXYPosition = true;
		UniTask.Yield().GetAwaiter().OnCompleted(delegate
		{
			emission1.enabled = true;
			emission2.enabled = false;
		});
	}

	public void SetHornLayer(bool value)
	{
		hornLRenderer.GetComponent<Renderer>().sortingLayerName = (value ? "VFX" : "Character");
	}
}
