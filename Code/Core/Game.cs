using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Library;
using SPR;
using SPR.Achievement;
using SPR.Cutscene;
using SPR.Graphic;
using SPR.Save;
using SPR.UI;
using SPR.UI.Ending;
using SPR.UI.Title;
using SPR.UI.Tutorial;
using StaticData;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
	private static Game instance;

	[SerializeField]
	private GameMode mode;

	private readonly Dictionary<string, Manager> managerDictionary = new Dictionary<string, Manager>();

	private static GameData gameData;

	private static int saveNum;

	private AsyncOperationHandle<SceneInstance> sceneLoadingHandle;

	private bool isInitialized;

	private SceneDataModel sceneDataModel;

	private PlayerController player;

	public static bool IsInitialized => instance != null;

	public static Game Instance
	{
		get
		{
			if (instance.IsNull())
			{
				GameObject gameObject = GameObject.Find("Game(Clone)");
				if (!gameObject.IsNull())
				{
					DebugLog.GeneralLog("Game 오브젝트가 이미 존재합니다. 해당 오브젝트를 사용합니다.");
					return gameObject.GetComponent<Game>();
				}
				AsyncOperationHandle assetHandle = AddressableHelper.GetAssetHandle("Game");
				assetHandle.WaitForCompletion();
				instance = UnityEngine.Object.Instantiate(assetHandle.Result.CastObject<GameObject>()).GetComponent<Game>();
				instance.Initialize();
			}
			return instance;
		}
	}

	public SaveData SaveData => gameData.GetSaveData(saveNum);

	public RuntimeData RuntimeData { get; private set; }

	public SettingData SettingData => gameData.SettingData;

	public GameState CurrentState { get; private set; }

	public GameMode Mode => mode;

	public static Language CurrentLanguage => Instance.SettingData.Language;

	public FieldID CurrentFieldID { get; private set; }

	public static DataManager DataManager => DataManager.Instance;

	public TitleManager TitleManager => GetManager<TitleManager>();

	public EndingManager EndingManager => GetManager<EndingManager>();

	public CreditManager CreditManager => GetManager<CreditManager>();

	public InputManager InputManager => GetManager<InputManager>();

	public UIManager UIManager => GetManager<UIManager>();

	public FieldController FieldController => GetManager<FieldController>();

	public ObjectPoolManager ObjectPoolManager => GetManager<ObjectPoolManager>();

	public GraphicManager GraphicManager => GetManager<GraphicManager>();

	public SoundManager SoundManager => GetManager<SoundManager>();

	public VibrationManager VibrationManager => GetManager<VibrationManager>();

	public DevController DevController => base.transform.GetComponentInChildren<DevController>();

	public PlayerController Player
	{
		get
		{
			if (!Application.isPlaying)
			{
				return null;
			}
			if (player.IsNull())
			{
				AsyncOperationHandle assetHandle = AddressableHelper.GetAssetHandle("Assets/Prefabs/Player.prefab");
				assetHandle.WaitForCompletion();
				player = UnityEngine.Object.Instantiate(assetHandle.Result.CastObject<GameObject>()).GetComponent<PlayerController>();
				player.InitializePlayer();
				player.SetPosition(Util.DisablePosition);
			}
			return player;
		}
	}

	public static int SaveNum
	{
		get
		{
			return saveNum;
		}
		set
		{
			if (value < 0 || value >= 9)
			{
				DebugLog.GeneralLogError($"{value}는 Save Number로서 적합하지 않습니다.");
			}
			else
			{
				saveNum = value;
			}
		}
	}

	public static FieldID FieldID
	{
		get
		{
			if (!Application.isPlaying)
			{
				return FieldID.GetEditorFieldID();
			}
			return Instance.CurrentFieldID;
		}
	}

	public static float GetTimeScale()
	{
		return Time.timeScale;
	}

	public static void SetTimeScale(float value)
	{
		Time.timeScale = value;
	}

	private void Initialize()
	{
		if (isInitialized)
		{
			return;
		}
		isInitialized = true;
		DebugManager.instance.enableRuntimeUI = false;
		Application.targetFrameRate = 60;
		LoadAndCacheGameData();
		RuntimeData = new RuntimeData();
		AchievementHelper.Initialize();
		managerDictionary.Clear();
		base.transform.GetComponentsInChildren<Manager>().ForEach(delegate(Manager manager)
		{
			managerDictionary.Add(manager.GetType().FullName, manager);
		});
		foreach (KeyValuePair<string, Manager> item in managerDictionary)
		{
			item.Value.Initialize();
		}
		SceneManager.sceneLoaded += delegate(Scene scene, LoadSceneMode _)
		{
			OnSceneLoaded(scene).Forget();
		};
		SceneManager.sceneUnloaded += OnSceneUnloaded;
	}

	public static void LoadAndCacheGameData()
	{
		gameData = SaveHelper.Load();
		gameData.SettingData.OnSaveLoaded();
		saveNum = 0;
		DebugLog.SaveDataLog("GameData 로드 완료. OwningUser: " + gameData.OwningUser);
	}

	private async UniTask OnSceneLoaded(Scene scene)
	{
		CurrentFieldID = FieldID.ConvertToFieldID(scene);
		CurrentState = GameState.SceneLoading;
		if (sceneDataModel.IsNull)
		{
			if (!FieldID.IsField(scene))
			{
				sceneDataModel = SceneDataModel.GetOutGameModel(scene.name);
			}
			else if (SaveData.IsNewSaveData && CurrentFieldID == FieldID.NewGameStartSceneID())
			{
				sceneDataModel = SceneDataModel.NewGameSceneDataModel();
			}
			else if (CurrentFieldID.Equals(SectorName.WorldsEnd, 90))
			{
				sceneDataModel = SceneDataModel.MoveCutsceneModel(CurrentFieldID, "Ending");
			}
			else
			{
				sceneDataModel = new SceneDataModel(scene);
			}
		}
		DebugLog.GeneralLog(scene.name + "으로 씬 전환 시작. OnSceneLoaded 호출됨");
		foreach (Manager value in managerDictionary.Values)
		{
			await value.OnSceneLoaded(scene);
		}
		DebugLog.GeneralLog($"{managerDictionary.Count} 개의 매니저가 모두 씬 전환 Task를 수행하였습니다.");
		await StartScene();
		if (CurrentFieldID.SectorName != SectorName.None)
		{
			float x = Player.mover.VisualBottom.x;
			UIManager.FieldCanvas.SectorBoard.TryAppear(CurrentFieldID.SectorName, SectorBoardAppearCondition(x));
			SaveData.WorldData.AddVisitedFieldID(CurrentFieldID);
			UIManager.MenuCanvas.MapPanel.CheckAchievementCleared();
			if (SaveData.WanderingData.TryDequeueResonanceAfterMoveScene(CurrentFieldID, out var groupId) && !VeritaCutsceneProducer.IsVeritaInfected)
			{
				UIManager.TalkCanvas.ShowSpecialTalk(groupId, 2f).Forget();
			}
			LazySingleton<MessageSystem>.Instance.Publish(new InflarePointTutorialEvent());
		}
		CurrentState = GameState.Initialized;
	}

	private Func<Vector2, bool> SectorBoardAppearCondition(float startX)
	{
		int distance = ((CurrentFieldID.SectorName != SectorName.Library) ? 30 : 0);
		if (CurrentFieldID.Equals(SectorName.WindshadeSpire, 25))
		{
			return (Vector2 playerPos) => Mathf.Abs(playerPos.x - 132f) > (float)distance && playerPos.y > 15f;
		}
		return (Vector2 playerPos) => Mathf.Abs(playerPos.x - startX) > (float)distance;
	}

	private void OnSceneUnloaded(Scene scene)
	{
		CurrentState = GameState.SceneLoading;
		if (FieldID.ConvertToFieldID(scene).SectorName != SectorName.None)
		{
			return;
		}
		Player.InitializePlayer();
		foreach (KeyValuePair<string, Manager> item in managerDictionary)
		{
			item.Value.Initialize(force: true);
		}
	}

	private async UniTask StartScene()
	{
		switch (sceneDataModel.SceneType)
		{
		case SceneType.Title:
			await StartTitleAsync();
			break;
		case SceneType.Field:
			await StartFieldAsync();
			break;
		case SceneType.Ending:
			await StartEndingAsync();
			break;
		case SceneType.Credit:
			await StartCreditAsync();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case SceneType.Splash:
			break;
		}
	}

	private async UniTask StartTitleAsync()
	{
		NullControllable nullControllable = null;
		await UIManager.FadeCanvas.FadeOut(0f);
		if (!InputManager.ContainsInControlStack(TitleManager.TitlePanel))
		{
			nullControllable = new NullControllable("Fade In");
			await InputManager.StackControlTarget(TitleManager.TitlePanel);
			await InputManager.StackControlTarget(nullControllable);
		}
		await UIManager.FadeCanvas.FadeIn(1f);
		if (nullControllable != null)
		{
			await InputManager.RemoveControlTarget(nullControllable);
		}
	}

	private async UniTask StartFieldAsync()
	{
		await UIManager.FadeCanvas.FadeOut(0f);
		if (!InputManager.ContainsInControlStack(Player))
		{
			await InputManager.StackControlTarget(Player);
		}
		if (sceneDataModel.SceneLoadType != SceneLoadType.MoveByOther && !sceneDataModel.SpecialKey.IsNullOrEmpty())
		{
			await FieldController.LoadSpecialCutsceneAsync(sceneDataModel.EntranceName, sceneDataModel.SpecialKey);
			return;
		}
		NullControllable nullControllable = new NullControllable("Spawn Player");
		await InputManager.StackControlTarget(nullControllable);
		switch (sceneDataModel.SceneLoadType)
		{
		case SceneLoadType.StartFromHere:
			await FieldController.SpawnRespawnPlayerAsync();
			Player.PlayerStat.UpdateFieldCanvas();
			UIManager.FieldCanvas.ShowAllAsync(instant: false).Forget();
			break;
		case SceneLoadType.ComeFromOther:
			await FieldController.SpawnMovingPlayerAsync(sceneDataModel.PlayerSpawnEntranceName);
			Player.PlayerStat.UpdateFieldCanvas();
			UIManager.FieldCanvas.ContinueCurrentState().Forget();
			break;
		case SceneLoadType.SaveFromHere:
			await FieldController.SpawnSavePlayerAsync();
			break;
		case SceneLoadType.MoveByOther:
			await FieldController.SpawnPlayerWithElevatorAsync(sceneDataModel.EntranceName, sceneDataModel.SpecialKey == "vertical");
			Player.PlayerStat.UpdateFieldCanvas();
			UIManager.FieldCanvas.ContinueCurrentState().Forget();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		await InputManager.RemoveControlTarget(nullControllable);
		FieldController.EnableEnemiesAI();
	}

	private async UniTask StartEndingAsync()
	{
		await UIManager.FadeCanvas.FadeIn(0f);
		if (!InputManager.ContainsInControlStack(EndingManager))
		{
			await InputManager.StackControlTarget(EndingManager);
		}
	}

	private async UniTask StartCreditAsync()
	{
		await UIManager.FadeCanvas.FadeIn(0f);
		if (!InputManager.ContainsInControlStack(CreditManager))
		{
			await InputManager.StackControlTarget(CreditManager);
		}
	}

	public async UniTask LoadTitleAsync()
	{
		NullControllable nullControllable = new NullControllable("Load Title");
		await InputManager.StackControlTarget(nullControllable);
		sceneDataModel = SceneDataModel.TitleModel();
		SoundManager.StopAMB();
		SoundManager.StopBGM();
		await UIManager.FadeCanvas.FadeOut(0.4f);
		Player.SetPosition(Util.DisablePosition);
		RuntimeData.ClearRuntimeData();
		ObjectPoolManager.ReturnAll();
		await LoadSceneAsync_Internal(sceneDataModel.SceneAddress);
		await InputManager.RemoveControlTarget(nullControllable);
	}

	public async UniTask LoadFieldAsync(SceneDataModel newModel, bool skipExit = false)
	{
		Player.IsInSceneLoadSequence = true;
		SoundManager.FadeOutSFXEmitters(0.5f);
		AddressableHelper.UnloadAsset("Assets/Prefabs/Player.prefab");
		AddressableHelper.UnloadAsset("Game");
		NullControllable nullControllable = new NullControllable("Change Stage");
		await InputManager.StackControlTarget(nullControllable);
		sceneDataModel = newModel;
		UniTask uniTask = ((skipExit || SkipExit(sceneDataModel.PlayerSpawnEntranceName) || Instance.DevController.IsDevMode) ? UniTask.CompletedTask : FieldController.ExitPlayerAsync(sceneDataModel.PlayerSpawnEntranceName));
		UniTask uniTask2 = UIManager.FadeCanvas.FadeOut(0.4f, 0.2f);
		await UniTask.WhenAll(uniTask, uniTask2);
		Player.SetPosition(Util.DisablePosition);
		SoundManager.SaveLastInfo(CurrentFieldID, Player.transform.position);
		ObjectPoolManager.ReturnAll();
		string address = sceneDataModel.SceneAddress;
		FieldID fieldID = FieldID.ConvertToFieldID(sceneDataModel.SceneAddress);
		if (Trigger.CheckTrigger("Trigger_LibraryDestroyed") && fieldID.Equals(SectorName.Library, 3))
		{
			address = new FieldID(SectorName.Library, 99).Address;
		}
		if (Trigger.CheckTrigger("Trigger_ShouldTeleportToLibraryFront") && (fieldID.SectorName == SectorName.Library || fieldID.Equals(SectorName.AshHills, 26)))
		{
			sceneDataModel = SceneDataModel.MoveCutsceneModel(new FieldID(SectorName.AshHills, 26), "MoldinClearVeritaEntrance_1");
			address = new FieldID(SectorName.AshHills, 26).Address;
		}
		await LoadSceneAsync_Internal(address);
		await InputManager.RemoveControlTarget(nullControllable);
		Player.IsInSceneLoadSequence = false;
		static bool SkipExit(string entranceName)
		{
			if (!entranceName.IsNullOrEmpty() && !entranceName.StartsWith("Portal"))
			{
				return entranceName.EndsWith("GiantElevator");
			}
			return true;
		}
	}

	public async UniTask LoadEndingAsync()
	{
		NullControllable nullControllable = new NullControllable("Load Ending");
		await InputManager.StackControlTarget(nullControllable);
		sceneDataModel = SceneDataModel.EndingModel();
		await UIManager.FadeCanvas.FadeOut(0.4f);
		Player.SetPosition(Util.DisablePosition);
		ObjectPoolManager.ReturnAll();
		await LoadSceneAsync_Internal(sceneDataModel.SceneAddress);
		await InputManager.RemoveControlTarget(nullControllable);
		if (InputManager.ContainsInControlStack(Player))
		{
			await InputManager.RemoveControlTarget(Player);
		}
	}

	public void SaveField()
	{
		RuntimeData.ClearRuntimeDictionaries();
		SaveBonfire();
	}

	public void SaveBonfire()
	{
		if (!FieldController.TryFindBonfire(out var _))
		{
			DebugLog.GeneralLogError($"{CurrentFieldID} 제단이 없는 씬을 세이브 데이터에 저장하려 시도하고 있습니다.");
			return;
		}
		SaveData.WorldData.AddInteractedBonfireFieldID(CurrentFieldID);
		SaveData.SetLastBonfireScene(CurrentFieldID);
		Save();
	}

	private async UniTask LoadSceneAsync_Internal(string address)
	{
		AsyncOperationHandle<SceneInstance> op = Addressables.LoadSceneAsync(address);
		await op.Task;
		if (op.Status != AsyncOperationStatus.Succeeded)
		{
			Debug.LogError(address + "씬 로딩 실패");
			return;
		}
		await UniTask.WaitUntil(() => CurrentState == GameState.Initialized);
	}

	public async UniTask Respawn(IAttackSource attackSource)
	{
		bool isDead = attackSource != null;
		if (isDead)
		{
			await UniTaskExtension.WaitForSeconds(0.5f);
		}
		await UIManager.FadeCanvas.FadeOut(0.4f);
		await UIManager.FadeCanvas.LoadingBoard.ShowAsync(attackSource);
		RuntimeData.ClearRuntimeDictionaries();
		await LoadFieldAsync(SceneDataModel.RespawnModel(SaveData.LastFieldID));
		if (!isDead)
		{
			return;
		}
		if (SaveData.WanderingData.ShouldShowTutorial(ActionName.FirstDeath))
		{
			UIManager.TutorialCanvas.SetData(ActionName.FirstDeath);
			InputManager.StackControlTarget(UIManager.TutorialCanvas).Forget();
			await UniTask.WaitUntil(() => InputManager.IsCurrentControlTarget(Instance.Player));
		}
		await RuntimeData.ShowStatusEffectTutorial();
	}

	public void Save()
	{
		gameData.SetSaveData(saveNum, SaveData);
		SaveHelper.Save(gameData);
	}

	public SaveData GetSaveData(int index)
	{
		return gameData.GetSaveData(index);
	}

	public void SetSaveData(SaveData data)
	{
		gameData.SetSaveData(saveNum, data);
	}

	public static void DeleteSave()
	{
		gameData.Reset();
		SaveHelper.Save(gameData);
	}

	public static void DeleteSave(int slotNum)
	{
		gameData.SetSaveData(slotNum, new SaveData());
		SaveHelper.Save(gameData);
	}

	public static bool IsWatchedCutsceneInGlobal(string name, int state)
	{
		return gameData.IsWatchedCutsceneInGlobal(name, state);
	}

	public void SetDifficulty(Difficulty difficulty, bool settingInGame)
	{
		if (settingInGame)
		{
			Player.PlayerStat.ApplyDifficulty(difficulty);
			Player.mover.ApplyDifficulty(difficulty);
		}
		SaveData.SetDifficulty(difficulty);
	}

	public static async UniTask QuitGameAsync()
	{
		Instance.InputManager.BlockControlForTask(UniTask.Never(CancellationToken.None), "Quit Game").Forget();
		await Instance.UIManager.FadeCanvas.FadeOut(1.2f);
		Application.Quit();
	}

	private T GetManager<T>() where T : Manager
	{
		string fullName = typeof(T).FullName;
		if (managerDictionary.TryGetValue(fullName, out var value))
		{
			return value as T;
		}
		DebugLog.GeneralLogWarning(fullName + "에 해당하는 매니저를 찾을 수 없습니다.");
		return null;
	}

	public void SetManager<T>(T manager) where T : Manager
	{
		string fullName = manager.GetType().FullName;
		if (!managerDictionary.TryAdd(fullName, manager))
		{
			DebugLog.GeneralLogError($"{manager}가 이미 존재합니다! 덮어씌웁니다.");
			managerDictionary[fullName] = manager;
		}
		else
		{
			DebugLog.GeneralLog(fullName + "을 등록했습니다.");
		}
	}

	public void RemoveManager<T>(T manager) where T : Manager
	{
		string fullName = manager.GetType().FullName;
		if (!managerDictionary.Remove(fullName))
		{
			DebugLog.GeneralLog(fullName + "을 찾을 수 없습니다.");
		}
		else
		{
			DebugLog.GeneralLog(fullName + "을 제거했습니다.");
		}
	}

	private void OnDestroy()
	{
		managerDictionary.Clear();
	}
}
