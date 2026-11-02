using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using Library;
using SPR.Save;
using SPR.UI.Map;
using SPR.UI.Talk;
using Spine;
using UnityEngine;

namespace SPR.Field;

public class Portal : ServiceObject, IStateSavable, IEntrance, IMappable
{
	public const int DefaultState = 0;

	public const int ActiveState = 1;

	[Header("Portal")]
	[SerializeField]
	private FieldID targetSceneID;

	[SerializeField]
	private Area connectArea;

	[SerializeField]
	private string address;

	[SerializeField]
	private bool canActivate = true;

	[Header("Sfx")]
	[SerializeField]
	private EventReference openInEvent;

	[SerializeField]
	private EventReference openOutEvent;

	[SerializeField]
	private EventReference walkInEvent;

	[SerializeField]
	private EventReference walkOutEvent;

	[SerializeField]
	private EventReference loopEvent;

	private BoxCollider2D coll;

	private SceneDataModel sceneDataModel;

	private EventInstance loopInstance;

	public int CurrentState { get; private set; }

	private bool CanActivate => canActivate;

	private string OppositeID => $"{targetSceneID}_{GetObjectName(Game.Instance.CurrentFieldID)}";

	public override string Name => "Portal";

	public string ID => $"{Game.FieldID}_{GetObjectName(targetSceneID)}";

	private string ScenarioID
	{
		get
		{
			if (CurrentState != 0 || CanActivate)
			{
				return "Portal_Use";
			}
			return "Portal_Unable";
		}
	}

	private BoxCollider2D Coll => coll ?? (coll = GetComponent<BoxCollider2D>());

	public bool IsInteracting { get; private set; }

	public Area ConnectArea => connectArea;

	public bool IsVertical => false;

	private static TalkCanvas TalkCanvas => Game.Instance.UIManager.TalkCanvas;

	private void Awake()
	{
		AddAnimationEvent(AnimState.Complete, OpenAnimationOnComplete);
	}

	private void OnDestroy()
	{
		RemoveAnimationEvent(AnimState.Complete, OpenAnimationOnComplete);
		if (loopInstance.isValid())
		{
			loopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			loopInstance.release();
		}
	}

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		this.CastObject<IStateSavable>().InitializeState();
		sceneDataModel = GetSceneDataModel();
		SetAnimation("background", loop: true, 1f, 2);
	}

	public void LoadState(int state)
	{
		CurrentState = state;
		switch (state)
		{
		case 0:
			SetAnimation("close_loop");
			SetAnimation("close_loop_acc", loop: true, 1f, 1);
			break;
		case 1:
			EnableVFX("loop");
			SetOpenLoop();
			break;
		}
	}

	public void ChangeState(int state)
	{
		if (state == 1)
		{
			CurrentState = state;
			if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
			{
				data.SaveObjectState(ID, state);
			}
		}
	}

	public override void Interact()
	{
		if (Game.Instance.SaveData.EnemyFightData.IsBossCleared(BossName.Imahen) && !Game.Instance.SaveData.WanderingData.IsWatchedCutscene("NpcRescueCutsceneProducer"))
		{
			Game.Instance.UIManager.FadeCanvas.WarningBoard.ShowWarning("UI_Warmth_Tunnel_Warning");
			return;
		}
		Game.Instance.SaveData.WorldData.AddInteractedFieldObject(ID);
		switch (CurrentState)
		{
		case 0:
			if (CanActivate)
			{
				ServiceObject.InputManager.BlockControlForTask(SetActiveAsync(canvasPopup: true), "Activate Portal").Forget();
				ChangeState(1);
			}
			break;
		case 1:
			base.Interact();
			break;
		}
	}

	private void OnInteractOver()
	{
		IsInteracting = false;
	}

	public async UniTask SetActiveAsync(bool canvasPopup)
	{
		Coll.enabled = false;
		if (GetCurrentAnim(0).Name != "open_loop")
		{
			SetAnimation("open", loop: false);
			SetAnimation("open_acc", loop: true, 1f, 1);
			ServiceObject.SoundManager.PlayOneShotAttached(canvasPopup ? openInEvent : openOutEvent, base.transform);
		}
		await UniTask.WaitForSeconds(GetAnimDuration("open"));
		if (canvasPopup)
		{
			base.Interact();
		}
		Coll.enabled = true;
	}

	public async UniTask MoveSceneAsync()
	{
		base.ServiceVcam.gameObject.SetActive(value: true);
		PlayerMover player = Game.Instance.Player.mover;
		ServiceObject.SoundManager.PlayOneShotAttached(walkInEvent, base.transform);
		await Game.Instance.Player.MoveXPositionAsync(base.Position.x, isRunning: false);
		await UniTask.WaitForSeconds(0.5f);
		player.SetAnimation("turn_back_2", loop: false);
		player.AddAnimation("walk_turn_back");
		await UniTask.WaitForSeconds(1.5f);
		LoadField();
	}

	private SceneDataModel GetSceneDataModel()
	{
		string objectName = GetObjectName(Game.Instance.CurrentFieldID);
		if (!address.IsNullOrEmpty())
		{
			return SceneDataModel.MoveSpecialModel(targetSceneID.SectorName, address, objectName);
		}
		return SceneDataModel.MoveModel(targetSceneID, objectName);
	}

	public Vector2 GetPlayerSpawnPosition()
	{
		return (Vector2)base.transform.position + Vector2.up;
	}

	public Vector2 GetPlayerEnterEndPosition()
	{
		return base.transform.position;
	}

	private void OnValidate()
	{
		base.name = GetObjectName(targetSceneID);
	}

	public static string GetObjectName(FieldID targetID)
	{
		return $"Portal_{targetID}";
	}

	public MapObjectInfo GetMapObjectInfo(Vector2 center)
	{
		Vector2 position = base.transform.position.ToVector2() - center;
		return new MapObjectInfo(MapObjectType.Portal, position, ID);
	}

	private void LoadField()
	{
		if (base.name.Contains("Ending"))
		{
			Game.Instance.LoadEndingAsync().Forget();
		}
		else
		{
			Game.Instance.LoadFieldAsync(sceneDataModel).Forget();
		}
	}

	private void OpenAnimationOnComplete(TrackEntry trackEntry)
	{
		if (trackEntry.Animation.Name == "open")
		{
			SetOpenLoop();
		}
	}

	private void SetOpenLoop()
	{
		SetAnimation("open_loop");
		SetAnimation("open_loop_acc", loop: true, 1f, 1);
		loopInstance = ServiceObject.SoundManager.PlayOneShotAttached(loopEvent, base.transform, forcedOnSceneLoad: true);
	}

	public void PlayWalkOutSfx()
	{
		ServiceObject.SoundManager.PlayOneShotAttached(walkOutEvent, base.transform, forcedOnSceneLoad: true);
	}

	GameObject IEntrance.gameObject => base.gameObject;
}
