using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Cysharp.Threading.Tasks;
using FSM;
using Library;
using SPR.Cutscene;
using SPR.Field;
using SPR.Library;
using SPR.Save;
using SPR.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SPR;

public class FieldController : Manager
{
	[SerializeField]
	private Tilemap platformTilemap;

	[SerializeField]
	private Transform entranceParent;

	[SerializeField]
	private List<Area> areas;

	private readonly List<Enemy> enemies = new List<Enemy>();

	private readonly List<Enemy> deadEnemies = new List<Enemy>();

	private List<IEntrance> entrances;

	private List<IInteractable> interactables = new List<IInteractable>();

	private List<CutsceneProducer> cutsceneProducers;

	private Platform platform;

	private AerialPath aerialPath;

	public IEnumerable<Enemy> Enemies => enemies;

	public IReadOnlyList<Enemy> DeadEnemies => deadEnemies;

	public Area CurrentArea { get; private set; }

	public bool IsLoaded { get; private set; }

	public Platform Platform => platform ?? (platform = GetComponentInChildren<Platform>());

	public Area MainArea => areas.First((Area area) => area.GetType() != typeof(SubArea));

	private static UIManager UIManager => Game.Instance.UIManager;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static PlayerController Player => Game.Instance.Player;

	private static SaveData SaveData => Game.Instance.SaveData;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private List<EnemyPlaceHolder> MonsterPlaceHolders => base.transform.GetComponentsInChildren<EnemyPlaceHolder>().ToList();

	private List<CutsceneProducer> CutsceneProducers => cutsceneProducers ?? (cutsceneProducers = base.transform.GetComponentsInChildren<CutsceneProducer>().ToList());

	public AerialPath AerialPath => aerialPath ?? (aerialPath = GetComponentInChildren<AerialPath>());

	public static Bonfire.VisualState BonfireVisualState { get; set; } = Bonfire.VisualState.Respawn;

	private void Awake()
	{
		Game.Instance.RemoveManager(this);
		Game.Instance.SetManager(this);
		AddressableHelper.PreloadAssetAsync("FieldInteractCanvas.prefab").Forget();
	}

	protected override void OnInitialize()
	{
	}

	public override async UniTask OnSceneLoaded(Scene loadedScene)
	{
		IsLoaded = false;
		SetCineBrainActive(value: true);
		PlayerData.DropData currentDropData = SaveData.PlayerData.CurrentDropData;
		if (currentDropData.IsValid && currentDropData.FieldID == Game.Instance.CurrentFieldID)
		{
			await AddressableHelper.InstantiateAsync<PlayerDropItem>("Player Drop Item.prefab", currentDropData.WorldPos, 0f, base.transform);
		}
		platformTilemap.CompressBounds();
		entrances = entranceParent.GetComponentsInChildren<IEntrance>().ToList();
		interactables = base.transform.GetComponentsInChildren<IInteractable>().ToList();
		SaveData saveData = SaveData;
		if (saveData.WorldData.TryGetSectorData(out var data))
		{
			foreach (string dropItemID in data.DropItemIDList)
			{
				var (fieldID, itemName, spawnPos) = Util.ParseID(dropItemID);
				if (!(Game.FieldID != fieldID))
				{
					ItemObject.SpawnItemAsync(itemName, spawnPos).Forget();
				}
			}
		}
		areas.ForEach(delegate(Area area)
		{
			area.Initialize(this);
		});
		interactables.ForEach(delegate(IInteractable interactable)
		{
			interactable.AwakeObject(saveData);
		});
		enemies.Clear();
		PathFinder.UpdateConnectedPath();
		await AddressableHelper.PreloadAssetAsync("Stun Point_Boss.prefab");
		int sortingOrder = 0;
		foreach (EnemyPlaceHolder monsterPlaceHolder in MonsterPlaceHolders)
		{
			if (monsterPlaceHolder.transform.parent.TryGetComponent<EnemyWave>(out var _) || !monsterPlaceHolder.IsAlreadyDeadCreature)
			{
				UniTask<Enemy> uniTask = monsterPlaceHolder.InstantiateCharacterAsync(sortingOrder++);
				List<Enemy> list = enemies;
				list.Add(await uniTask);
			}
		}
		await UniTask.WaitUntil(() => !UIManager.IsNull() && !InputManager.IsNull() && !Player.IsNull());
		IsLoaded = true;
	}

	private void OnDestroy()
	{
		Game.Instance.RemoveManager(this);
	}

	public async UniTask LoadSpecialCutsceneAsync(string cutsceneName, string specialKey)
	{
		int result = 0;
		string[] array = cutsceneName.Split('_');
		if (array.Length > 1)
		{
			cutsceneName = array[0];
			array[1].TryParseInt(out result);
		}
		switch (specialKey)
		{
		case "PlayerBorn":
			await FindCutsceneProducer<PlayerBornCutsceneProducer>().PlayTimelineAsync(result, blockControl: true);
			break;
		case "Cutscene":
		{
			CutsceneProducer cutsceneProducer = FindCutsceneProducer(cutsceneName);
			if ((object)cutsceneProducer == null)
			{
				DebugLog.GeneralLogError("특수 컷신을 찾을 수 없습니다.");
			}
			else
			{
				await cutsceneProducer.PlayTimelineAsync(result, blockControl: true);
			}
			break;
		}
		default:
			DebugLog.GeneralLogError("특수 컷신을 찾을 수 없습니다.");
			break;
		case "Portal":
			break;
		}
	}

	public async UniTask SpawnRespawnPlayerAsync()
	{
		if (!TryFindBonfire(out var _))
		{
			DebugLog.GeneralLogError($"{Game.Instance.CurrentFieldID}에 모닥불이 없습니다.");
			await SpawnMovingPlayerAsync(string.Empty);
		}
		else
		{
			Player.SetPosition(Util.DisablePosition);
			Player.PlayerStat.RecoverStat(isHeatFulled: true);
			await FadeInAndSpawnAtBonfireAsync();
		}
	}

	public async UniTask SpawnMovingPlayerAsync(string entranceName)
	{
		IEntrance entrance = FindEntrance(entranceName);
		ChangeAreaTo(entrance.ConnectArea);
		Player.SetPosition(entrance.GetPlayerSpawnPosition(), Player.mover.IsFacingRight);
		SoundManager.Set3DAttributeOnSceneLoading();
		if (TryFindBonfire(out var bonfire))
		{
			bonfire.LoadDefaultState();
		}
		float num = ((entrance is OneWayEntrance) ? 2f : ((entrance is GiantElevator) ? 1.5f : ((!(entrance is Entrance { name: "we_02 - we_03" })) ? 0.4f : 2f)));
		float duration = num;
		UniTask fadeTask = UIManager.FadeCanvas.FadeIn(duration);
		UniTask playerMoveTask = UniTask.CompletedTask;
		if (Game.Instance.DevController.IsDevMode)
		{
			Player.SetPosition(entrance.GetPlayerEnterEndPosition());
		}
		else if (entrance is OtherEntrance { name: "we_02 - we_03" } && Game.FieldID.Equals(new FieldID(SectorName.WorldsEnd, 2)))
		{
			Player.mover.SetAnimation("idle_back");
			await UniTask.WaitForSeconds(1f);
			Player.mover.SetAnimation("turn_front", loop: false);
			Player.mover.AddAnimation("idle");
		}
		else if (entrance is Entrance { name: "we_02 - we_03" })
		{
			Vector2 position = entrance.GetPlayerEnterEndPosition() + new Vector2(20f, 0f);
			Player.mover.Position = position;
			Player.mover.SetAnimationInstantly("cam_down_loop");
			await UniTask.WaitForSeconds(2f);
			Player.mover.SetAnimation("idle");
			await UniTask.WaitForSeconds(0.5f);
			Game.Instance.UIManager.FieldCanvas.ShowAllAsync(instant: false).Forget();
		}
		else if (entrance is Entrance entrance4)
		{
			playerMoveTask = entrance4.MovePlayerOnEnterAsync();
		}
		else if (entrance is Portal portal)
		{
			portal.ServiceVcam.gameObject.SetActive(value: true);
			Player.mover.SkeletonRenderer.Skeleton.SetAlpha(0f);
			if (portal.CurrentState == 0)
			{
				Player.gameObject.SetActive(value: false);
				await portal.SetActiveAsync(canvasPopup: false);
				Player.gameObject.SetActive(value: true);
			}
			Player.mover.SkeletonRenderer.Skeleton.DOAlpha(1f, 1f);
			Player.mover.SetAnimationInstantly("walk_front");
			portal.PlayWalkOutSfx();
			await UniTask.WaitForSeconds(1f);
			Player.mover.SetAnimation("turn_idle", loop: false);
			await UniTask.WaitUntil(() => Player.mover.IsAnimComplete(0, "turn_idle"));
			Player.mover.SetAnimation("idle");
			portal.ServiceVcam.gameObject.SetActive(value: false);
			portal.ChangeState(1);
		}
		else if (entrance is OneWayEntrance oneWayEntrance)
		{
			Player.mover.Flip(oneWayEntrance.Direction);
			Player.mover.SetAnimation("idle");
		}
		else
		{
			Player.mover.SetAnimationInstantly("idle");
			playerMoveTask = Player.MovePositionAsync(entrance.GetPlayerEnterEndPosition(), isRunning: true, silentY: false);
		}
		await UniTask.WhenAll(fadeTask, playerMoveTask);
	}

	public async UniTask SpawnPlayerWithElevatorAsync(string entranceName, bool isVertical)
	{
		IEntrance targetEntrance = FindEntrance(entranceName);
		ChangeAreaTo(targetEntrance.ConnectArea);
		Vector2 position = ((targetEntrance is ElevatorEntrance elevatorEntrance) ? elevatorEntrance.GetElevatorGroundPosition() : ((targetEntrance is HorizontalElevatorEntrance horizontalElevatorEntrance) ? horizontalElevatorEntrance.GetElevatorGroundPosition() : targetEntrance.GetPlayerEnterEndPosition()));
		Player.SetPosition(position, Player.mover.IsFacingRight);
		SoundManager.Set3DAttributeOnSceneLoading();
		if (TryFindBonfire(out var bonfire))
		{
			bonfire.LoadDefaultState();
		}
		UniTask uniTask = UIManager.FadeCanvas.FadeIn(0.4f);
		UniTask uniTask2 = (isVertical ? FindEntranceElevator(targetEntrance.ConnectArea).MoveOnEnterAsync() : FindHorizontalElevator(targetEntrance.ConnectArea).MoveOnEnterAsync());
		await UniTask.WhenAll(uniTask, uniTask2);
		if (!targetEntrance.IsVertical)
		{
			await UniTask.WaitUntil(() => Player.mover.IsOnGround);
		}
	}

	public async UniTask ExitPlayerAsync(string entranceName)
	{
		IEntrance entrance = FindEntrance(entranceName);
		if (entrance is OtherEntrance || entrance is OneWayEntrance)
		{
			return;
		}
		if (entrance.IsVertical)
		{
			if (!Player.mover.IsOnGround)
			{
				Player.mover.PSM.ChangeState(Player.mover.PSM.FallState);
			}
			Vector2 playerSpawnPosition = entrance.GetPlayerSpawnPosition();
			await Player.MoveXPositionAsync(playerSpawnPosition.x, isRunning: true);
			return;
		}
		float y = entrance.GetPlayerSpawnPosition().y;
		PlayerMover mover = Player.mover;
		if (y > Player.transform.position.y)
		{
			mover.PSM.ChangeState(mover.PSM.JumpUpState);
		}
		else if (mover.PSM.CurrentState.CastObject<PlayerState>().CanExitOnSceneLoading)
		{
			mover.JumpCancel();
			mover.PSM.ChangeState(mover.PSM.JumpDownState);
			mover.SetAnimation("jump_down_loop");
		}
		await Player.MoveYPositionAsync(y);
	}

	public async UniTask SpawnSavePlayerAsync()
	{
		if (!TryFindBonfire(out var bonfire))
		{
			DebugLog.GeneralLogError($"{Game.Instance.CurrentFieldID}에 모닥불이 없습니다.");
			await SpawnMovingPlayerAsync(string.Empty);
			return;
		}
		ChangeAreaTo(bonfire.ConnectArea);
		bonfire.SetVcamActive(3);
		bonfire.SetVisualActive(Bonfire.VisualState.Activate);
		Player.SetPosition(Player.mover.VisualCenter);
		Player.mover.Flip((Player.mover.VisualCenter.x < bonfire.CenterX) ? Direction.Right : Direction.Left);
		Player.mover.SetAnimationInstantly("sleep_loop");
		await InputManager.StackControlTarget(UIManager.RestCanvas);
		await UIManager.FadeCanvas.FadeIn(0.5f);
		bonfire.SetVisualActive(Bonfire.VisualState.Activate);
	}

	public async UniTask ExitSavePlayerAsync()
	{
		if (!TryFindBonfire(out var bonfire))
		{
			DebugLog.GeneralLogError($"{Game.Instance.CurrentFieldID}에 모닥불이 없습니다.");
			return;
		}
		bonfire.SetVcamActive(0);
		Player.mover.PSM.ChangeState(Player.mover.PSM.IdleState);
		Player.PlayerStat.ResetHeatGauge(isFulled: true);
		await UniTask.CompletedTask;
	}

	private async UniTask FadeInAndSpawnAtBonfireAsync()
	{
		if (TryFindBonfire(out var bonfire))
		{
			Game.Instance.SaveField();
			PlayerStateMachine psm = Player.mover.PSM;
			ChangeAreaTo(bonfire.ConnectArea);
			bonfire.SetVcamActive(1);
			bonfire.SetVisualActive(BonfireVisualState);
			UIManager.FadeCanvas.LoadingBoard.HideAsync(0.4f, 0.5f).Forget();
			await UIManager.FadeCanvas.FadeIn(0.4f, 0.5f);
			bonfire.SetVisualActive(BonfireVisualState);
			bonfire.PlayRespawnTimeline();
			await UniTaskExtension.WaitForSeconds(1.75f);
			Player.SetPosition(bonfire.PlayerPositionOnRespawnStart);
			Player.mover.PSM.ChangeState(psm.RespawnState);
			SoundManager.Set3DAttributeOnSceneLoading();
			await UniTask.WaitUntil(() => psm.CurrentState == psm.IdleState);
			bonfire.LoadDefaultState();
			BonfireVisualState = Bonfire.VisualState.Respawn;
		}
	}

	public bool TryFindBonfire(out Bonfire bonfire)
	{
		bonfire = interactables.Find((IInteractable interactable) => interactable is Bonfire) as Bonfire;
		return !bonfire.IsNull();
	}

	public bool TryFindAreaByPosition(Vector2 position, out Area area)
	{
		area = areas.FirstOrDefault((Area x) => x.IsInArea(position));
		return !area.IsNull();
	}

	public void ChangeAreaTo(Area area)
	{
		if (!CurrentArea.IsNull())
		{
			CurrentArea.OnAreaExit();
		}
		area.OnAreaEnter();
		CurrentArea = area;
	}

	public void AddEnemy(Enemy enemy)
	{
		enemies.Add(enemy);
		deadEnemies.Remove(enemy);
	}

	public void RemoveEnemy(Enemy enemy)
	{
		enemies.Remove(enemy);
		if (!enemy.IgnoreDeadList)
		{
			deadEnemies.Add(enemy);
		}
	}

	public void PauseField(bool value)
	{
	}

	private IEntrance FindEntrance(string entranceName)
	{
		if (string.IsNullOrEmpty(entranceName))
		{
			return entrances.First();
		}
		IEntrance entrance = entrances.Find((IEntrance entrance2) => entrance2.gameObject.name == entranceName);
		if (entrance != null)
		{
			return entrance;
		}
		DebugLog.GeneralLogError("다음 이름을 가진 출입구를 찾을 수 없습니다! : " + entranceName);
		return null;
	}

	private EntranceElevator FindEntranceElevator(Area area)
	{
		return area.GetComponentInChildren<EntranceElevator>();
	}

	private HorizontalElevator FindHorizontalElevator(Area area)
	{
		return area.GetComponentInChildren<HorizontalElevator>(includeInactive: true);
	}

	public T FindCutsceneProducer<T>() where T : CutsceneProducer
	{
		string text = typeof(T).Name;
		foreach (CutsceneProducer cutsceneProducer in CutsceneProducers)
		{
			if (text == cutsceneProducer.ControllerName())
			{
				return (T)cutsceneProducer;
			}
		}
		DebugLog.GeneralLogError(text + "을 찾을 수 없습니다.");
		return null;
	}

	public CutsceneProducer FindCutsceneProducer(string targetName)
	{
		string text = targetName + "CutsceneProducer";
		foreach (CutsceneProducer cutsceneProducer in CutsceneProducers)
		{
			if (text == cutsceneProducer.ControllerName())
			{
				return cutsceneProducer;
			}
		}
		DebugLog.GeneralLogError(text + "을 찾을 수 없습니다.");
		return null;
	}

	public IReadOnlyList<CutsceneProducer> GetCutsceneProducers()
	{
		return CutsceneProducers;
	}

	public Npc FindNpc(string npcName)
	{
		Npc obj = interactables.Find((IInteractable interactable) => interactable.Name == npcName) as Npc;
		if (obj.IsNull())
		{
			DebugLog.GeneralLogError(npcName + "을 찾을 수 없습니다.");
		}
		return obj;
	}

	public bool TryFindInteractable<T>(out T target, Func<T, bool> condition = null) where T : IInteractable
	{
		target = default(T);
		foreach (IInteractable interactable in interactables)
		{
			if (interactable is T val && !val.IsNull && (condition == null || condition(val)))
			{
				target = val;
				return true;
			}
		}
		DebugLog.GeneralLogError(typeof(T).Name + "을 찾을 수 없습니다.");
		return false;
	}

	public bool TryFindStateSavableObject<T>(out T target, Func<T, bool> condition = null) where T : IStateSavable
	{
		foreach (Area area in areas)
		{
			if (area.TryGetStateSavableObject<T>(out target) && condition != null && condition(target))
			{
				return true;
			}
		}
		DebugLog.GeneralLogError(typeof(T).Name + " 타입의 StateSavableObject를 찾을 수 없습니다.");
		target = default(T);
		return false;
	}

	public void EnableEnemiesAI()
	{
		enemies.Where((Enemy e) => e.gameObject.activeInHierarchy && e is Creature).ForEach(delegate(Enemy c)
		{
			c.Activate();
		});
	}

	public void DisableEntrance()
	{
		entranceParent.gameObject.SetActive(value: false);
	}

	private void SetCineBrainActive(bool value)
	{
		Camera main = Camera.main;
		if (main.IsNull())
		{
			DebugLog.GeneralLogError("메인 카메라를 찾을 수 없습니다.");
			return;
		}
		CinemachineBrain component = main.GetComponent<CinemachineBrain>();
		if (component.IsNull())
		{
			DebugLog.GeneralLogError("메인 카메라에 CinemachineBrain 컴포넌트가 붙어있지 않습니다.");
		}
		else
		{
			component.enabled = value;
		}
	}

	public void ModifyPathGraphs(List<Vector2> grounds, bool setGround, bool updateCreatures = true)
	{
		Platform.PathFinder.ModifyGraph(grounds, LayerUtility.PlatformLayer, setGround);
		if (updateCreatures)
		{
			PathFinder.UpdateConnectedPath();
			enemies.Where((Enemy x) => !x.IsDead && x is Creature).ForEach(delegate(Enemy x)
			{
				x.CastObject<Creature>().SetConnectedPath();
			});
		}
	}

	public void AddInteractable(IInteractable interactable)
	{
		if (!interactables.Contains(interactable))
		{
			interactables.Add(interactable);
		}
	}
}
