using System;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Cutscene;

public class MoldinClearVeritaCutsceneProducer : CutsceneProducer
{
	public enum State
	{
		None,
		CutsceneStart,
		CutsceneEnd,
		Watched
	}

	private const string LibraryFront = "AshHills_26";

	private const string VeritaOffice = "Library_01";

	private const string Library = "Library_04";

	public const string EntranceID = "MoldinClearVeritaEntrance";

	public const string OfficeID = "MoldinClearVeritaOffice";

	public const string PortalID = "MoldinClearVeritaPortal";

	[Header("Timeline Assets")]
	[SerializeField]
	private PlayableAsset libraryFrontTimeline;

	[SerializeField]
	private PlayableAsset veritaOfficeTimeline;

	[SerializeField]
	private PlayableAsset libraryTimeline;

	[Header("References")]
	[SerializeField]
	private GameObject totFigure;

	[SerializeField]
	private Transform teleportedPosition;

	[SerializeField]
	private CinemachineVirtualCamera endVcam;

	public override string ID => FieldID switch
	{
		"AshHills_26" => "MoldinClearVeritaEntranceCutsceneProducer", 
		"Library_01" => "MoldinClearVeritaOfficeCutsceneProducer", 
		"Library_04" => "MoldinClearVeritaPortalCutsceneProducer", 
		_ => base.ID, 
	};

	private static string FieldID => Game.Instance.CurrentFieldID.ToString();

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

	public override bool ShouldLoop(int index)
	{
		if ((uint)index <= 1u)
		{
			return CutsceneProducer.IsTalking;
		}
		return false;
	}

	public override void EnterLoop(int index)
	{
		if ((uint)index <= 1u)
		{
			StartScenarioInsideCutscene(index);
		}
	}

	public override void ExitLoop(int index)
	{
		_ = 1;
	}

	protected override void SetProducerState(int state)
	{
		if (!Game.Instance.SaveData.EnemyFightData.IsBossCleared(BossName.Moldin))
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		switch ((State)state)
		{
		case State.CutsceneEnd:
			if (FieldID == "AshHills_26")
			{
				ChangeState(3);
				base.gameObject.SetActive(value: false);
			}
			break;
		case State.Watched:
			base.gameObject.SetActive(value: false);
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		case State.None:
		case State.CutsceneStart:
			break;
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		switch ((State)state)
		{
		case State.None:
		{
			string fieldID = FieldID;
			if (fieldID == "Library_01" || fieldID == "Library_04")
			{
				await PlayTimelineAsync(1);
			}
			break;
		}
		case State.CutsceneStart:
			CutsceneProducer.Field.ChangeAreaTo(base.MyArea);
			switch (FieldID)
			{
			case "AshHills_26":
				base.PlayableDirector.playableAsset = libraryFrontTimeline;
				base.PlayableDirector.Evaluate();
				await CutsceneProducer.FadeCanvas.FadeIn(2f, 0f, Ease.Linear, token);
				break;
			case "Library_01":
				base.PlayableDirector.playableAsset = veritaOfficeTimeline;
				base.PlayableDirector.Evaluate();
				await CutsceneProducer.FadeCanvas.FadeIn(2f, 1f, Ease.Linear, token);
				break;
			case "Library_04":
				base.PlayableDirector.playableAsset = libraryTimeline;
				base.PlayableDirector.Evaluate();
				CutsceneProducer.Player.transform.position = teleportedPosition.position;
				CutsceneProducer.Player.mover.SetAnimationInstantly("idle");
				CutsceneProducer.Player.mover.PSM.ChangeState(CutsceneProducer.Player.mover.PSM.IdleState);
				SynchronizePlayer(totFigure.transform);
				BindPlayerTracks(value: true);
				await CutsceneProducer.FadeCanvas.FadeIn(2f, 1f, Ease.Linear, token);
				break;
			default:
				base.PlayableDirector.playableAsset = base.PlayableDirector.playableAsset;
				break;
			}
			break;
		case State.Watched:
			base.gameObject.SetActive(value: false);
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		case State.CutsceneEnd:
			break;
		}
	}

	protected override void OnEndTimeline(int state)
	{
		switch ((State)state)
		{
		case State.CutsceneStart:
			switch (FieldID)
			{
			case "AshHills_26":
				MoveLibrary01().Forget();
				CutsceneProducer.NotShowFieldCanvasUntilSceneChange();
				break;
			case "Library_01":
				MoveLibrary04().Forget();
				CutsceneProducer.NotShowFieldCanvasUntilSceneChange();
				break;
			case "Library_04":
				base.PlayableDirector.Stop();
				BindPlayerTracks(value: false);
				ChangeState(2);
				SynchronizeEndVcamAsync(endVcam).Forget();
				break;
			}
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		case State.None:
		case State.CutsceneEnd:
		case State.Watched:
			break;
		}
	}

	protected override void OnSkipTimeline(int state)
	{
		if (!FieldID.Equals("Library_04"))
		{
			DisableFadeInAfterSkip();
		}
	}

	private async UniTaskVoid WaitForPlayerControlStack()
	{
		await UniTask.WaitUntil(() => CutsceneProducer.InputManager.IsCurrentControlTarget(CutsceneProducer.Player));
	}

	private static async UniTaskVoid MoveLibrary01()
	{
		await CutsceneProducer.FadeCanvas.FadeOut(1f);
		await UniTask.Yield();
		Game.Instance.LoadFieldAsync(SceneDataModel.MoveCutsceneModel(new FieldID(SectorName.Library, 1), "MoldinClearVeritaOffice_1")).Forget();
	}

	private static async UniTaskVoid MoveLibrary04()
	{
		await CutsceneProducer.FadeCanvas.FadeOut(1f);
		Game.Instance.LoadFieldAsync(SceneDataModel.MoveCutsceneModel(new FieldID(SectorName.Library, 4), "MoldinClearVeritaPortal_1")).Forget();
	}
}
