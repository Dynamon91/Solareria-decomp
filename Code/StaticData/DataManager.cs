using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using StaticData;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DataManager
{
	private static DataManager instance;

	private AsyncOperationHandle<ScriptableObject> opHandle;

	private PlayerStatDataContainer playerStatDataContainer;

	private ItemDataContainer itemDataContainer;

	private LocaleDataContainer localeDataContainer;

	private EnemyDataContainer enemyDataContainer;

	private StoreItemDataContainer storeItemDataContainer;

	private ScenarioDataContainer scenarioDataContainer;

	private TriggerDataContainer triggerDataContainer;

	private EarthRewardDataContainer earthRewardDataContainer;

	private WaterRewardDataContainer waterRewardDataContainer;

	private FireRewardDataContainer fireRewardDataContainer;

	private WindRewardDataContainer windRewardDataContainer;

	private SporeRewardDataContainer sporeRewardDataContainer;

	private ShaftRewardDataContainer shaftRewardDataContainer;

	private BlacklistDataContainer blacklistDataContainer;

	private PlayerPartDataContainer playerPartDataContainer;

	private DifficultyDataContainer difficultyDataContainer;

	private MapNoticeDataContainer mapNoticeDataContainer;

	private CreditDataContainer creditDataContainer;

	private Dictionary<string, string> cookableIngredientDictionary;

	private Dictionary<string, List<string>> recipeIngredientDictionary;

	public static DataManager Instance => instance ?? (instance = new DataManager());

	public IReadOnlyList<PartData> PartDataList => playerPartDataContainer.PartDataList;

	public IReadOnlyList<CreditData> CreditDataList => creditDataContainer.CreditDataList;

	public IReadOnlyList<FoodData> FoodDataList => itemDataContainer.FoodDataList;

	private DataManager()
	{
		Initialize();
	}

	private void Initialize()
	{
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/PlayerStatDataContainer.asset");
		opHandle.WaitForCompletion();
		playerStatDataContainer = opHandle.Result as PlayerStatDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/ItemDataContainer.asset");
		opHandle.WaitForCompletion();
		itemDataContainer = opHandle.Result as ItemDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/LocaleDataContainer.asset");
		opHandle.WaitForCompletion();
		localeDataContainer = opHandle.Result as LocaleDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/EnemyDataContainer.asset");
		opHandle.WaitForCompletion();
		enemyDataContainer = opHandle.Result as EnemyDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/StoreItemDataContainer.asset");
		opHandle.WaitForCompletion();
		storeItemDataContainer = opHandle.Result as StoreItemDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/ScenarioDataContainer.asset");
		opHandle.WaitForCompletion();
		scenarioDataContainer = opHandle.Result as ScenarioDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/TriggerDataContainer.asset");
		opHandle.WaitForCompletion();
		triggerDataContainer = opHandle.Result as TriggerDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/EarthRewardDataContainer.asset");
		opHandle.WaitForCompletion();
		earthRewardDataContainer = opHandle.Result as EarthRewardDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/WaterRewardDataContainer.asset");
		opHandle.WaitForCompletion();
		waterRewardDataContainer = opHandle.Result as WaterRewardDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/FireRewardDataContainer.asset");
		opHandle.WaitForCompletion();
		fireRewardDataContainer = opHandle.Result as FireRewardDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/WindRewardDataContainer.asset");
		opHandle.WaitForCompletion();
		windRewardDataContainer = opHandle.Result as WindRewardDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/SporeRewardDataContainer.asset");
		opHandle.WaitForCompletion();
		sporeRewardDataContainer = opHandle.Result as SporeRewardDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/ShaftRewardDataContainer.asset");
		opHandle.WaitForCompletion();
		shaftRewardDataContainer = opHandle.Result as ShaftRewardDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/BlacklistDataContainer.asset");
		opHandle.WaitForCompletion();
		blacklistDataContainer = opHandle.Result as BlacklistDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/PlayerPartDataContainer.asset");
		opHandle.WaitForCompletion();
		playerPartDataContainer = opHandle.Result as PlayerPartDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/DifficultyDataContainer.asset");
		opHandle.WaitForCompletion();
		difficultyDataContainer = opHandle.Result as DifficultyDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/MapNoticeDataContainer.asset");
		opHandle.WaitForCompletion();
		mapNoticeDataContainer = opHandle.Result as MapNoticeDataContainer;
		opHandle = Addressables.LoadAssetAsync<ScriptableObject>("Assets/StaticData/CreditDataContainer.asset");
		opHandle.WaitForCompletion();
		creditDataContainer = opHandle.Result as CreditDataContainer;
	}

	public FlameData GetFlameData(string id)
	{
		return playerStatDataContainer.FlameDataList.FirstOrDefault((FlameData x) => x.ID == id);
	}

	public PlayerBurnData GetPlayerBurnData(string id)
	{
		return playerStatDataContainer.PlayerBurnDataList.FirstOrDefault((PlayerBurnData x) => x.ID == id);
	}

	public SkillData GetSkillData(string id)
	{
		return playerStatDataContainer.SkillDataList.FirstOrDefault((SkillData x) => x.ID == id);
	}

	public TraitData GetTraitData(int id)
	{
		return playerStatDataContainer.TraitDataList.FirstOrDefault((TraitData x) => x.ID == id);
	}

	public ItemData GetItemData(string id)
	{
		return itemDataContainer.ItemDataList.FirstOrDefault((ItemData x) => x.ID == id);
	}

	public FoodData GetFoodData(string id)
	{
		return itemDataContainer.FoodDataList.FirstOrDefault((FoodData x) => x.ID == id);
	}

	public LocaleData GetLocaleData(string id)
	{
		return localeDataContainer.LocaleDataList.FirstOrDefault((LocaleData x) => x.ID == id);
	}

	public LocaleData GetItemLocaleData(string id)
	{
		return localeDataContainer.ItemLocaleDataList.FirstOrDefault((LocaleData x) => x.ID == id);
	}

	public LocaleData GetStoreData(string id)
	{
		return localeDataContainer.StoreDataList.FirstOrDefault((LocaleData x) => x.ID == id);
	}

	public LocaleData GetFieldInteractData(string id)
	{
		return localeDataContainer.FieldInteractDataList.FirstOrDefault((LocaleData x) => x.ID == id);
	}

	public LocaleData GetMemoryData(string id)
	{
		return localeDataContainer.MemoryDataList.FirstOrDefault((LocaleData x) => x.ID == id);
	}

	public LocaleData GetAchievementData(string id)
	{
		return localeDataContainer.AchievementDataList.FirstOrDefault((LocaleData x) => x.ID == id);
	}

	public EnemyData GetEnemyData(string id)
	{
		return enemyDataContainer.EnemyDataList.FirstOrDefault((EnemyData x) => x.ID == id);
	}

	public EnemyStatData GetEnemyStatData(string id)
	{
		return enemyDataContainer.EnemyStatDataList.FirstOrDefault((EnemyStatData x) => x.ID == id);
	}

	public Enemy2Data GetEnemy2Data(string id)
	{
		return enemyDataContainer.Enemy2DataList.FirstOrDefault((Enemy2Data x) => x.ID == id);
	}

	public EnemyStat2Data GetEnemyStat2Data(string id)
	{
		return enemyDataContainer.EnemyStat2DataList.FirstOrDefault((EnemyStat2Data x) => x.ID == id);
	}

	public Enemy3Data GetEnemy3Data(string id)
	{
		return enemyDataContainer.Enemy3DataList.FirstOrDefault((Enemy3Data x) => x.ID == id);
	}

	public EnemyStat3Data GetEnemyStat3Data(string id)
	{
		return enemyDataContainer.EnemyStat3DataList.FirstOrDefault((EnemyStat3Data x) => x.ID == id);
	}

	public EnemySporeData GetEnemySporeData(string id)
	{
		return enemyDataContainer.EnemySporeDataList.FirstOrDefault((EnemySporeData x) => x.ID == id);
	}

	public EnemyWetData GetEnemyWetData(string id)
	{
		return enemyDataContainer.EnemyWetDataList.FirstOrDefault((EnemyWetData x) => x.ID == id);
	}

	public EnemyDropData GetEnemyDropData(string id)
	{
		return enemyDataContainer.EnemyDropDataList.FirstOrDefault((EnemyDropData x) => x.ID == id);
	}

	public EnemyDrop2Data GetEnemyDrop2Data(string id)
	{
		return enemyDataContainer.EnemyDrop2DataList.FirstOrDefault((EnemyDrop2Data x) => x.ID == id);
	}

	public EnemyDrop3Data GetEnemyDrop3Data(string id)
	{
		return enemyDataContainer.EnemyDrop3DataList.FirstOrDefault((EnemyDrop3Data x) => x.ID == id);
	}

	public EnemyMultiplierData GetEnemyMultiplierData(int clearcount)
	{
		return enemyDataContainer.EnemyMultiplierDataList.FirstOrDefault((EnemyMultiplierData x) => x.ClearCount == clearcount);
	}

	public StoreItemData GetLeafaData(string id)
	{
		return storeItemDataContainer.LeafaDataList.FirstOrDefault((StoreItemData x) => x.ID == id);
	}

	public StoreItemData GetJimaData(string id)
	{
		return storeItemDataContainer.JimaDataList.FirstOrDefault((StoreItemData x) => x.ID == id);
	}

	public StoreItemData GetPwoongData(string id)
	{
		return storeItemDataContainer.PwoongDataList.FirstOrDefault((StoreItemData x) => x.ID == id);
	}

	public StoreItemData GetCardamineData(string id)
	{
		return storeItemDataContainer.CardamineDataList.FirstOrDefault((StoreItemData x) => x.ID == id);
	}

	public StoreItemData GetMuhaData(string id)
	{
		return storeItemDataContainer.MuhaDataList.FirstOrDefault((StoreItemData x) => x.ID == id);
	}

	public StoreItemData GetTouristPotData(string id)
	{
		return storeItemDataContainer.TouristPotDataList.FirstOrDefault((StoreItemData x) => x.ID == id);
	}

	public StoreItemData GetGemExchangerData(string id)
	{
		return storeItemDataContainer.GemExchangerDataList.FirstOrDefault((StoreItemData x) => x.ID == id);
	}

	public ScenarioData GetScenarioData(string id)
	{
		return scenarioDataContainer.ScenarioDataList.FirstOrDefault((ScenarioData x) => x.ID == id);
	}

	public TriggerData GetTriggerData(string id)
	{
		return triggerDataContainer.TriggerDataList.FirstOrDefault((TriggerData x) => x.ID == id);
	}

	public SproutRewardData GetEarthRewardData(int id)
	{
		return earthRewardDataContainer.EarthRewardDataList.FirstOrDefault((SproutRewardData x) => x.ID == id);
	}

	public SproutRewardData GetWaterRewardData(int id)
	{
		return waterRewardDataContainer.WaterRewardDataList.FirstOrDefault((SproutRewardData x) => x.ID == id);
	}

	public SproutRewardData GetFireRewardData(int id)
	{
		return fireRewardDataContainer.FireRewardDataList.FirstOrDefault((SproutRewardData x) => x.ID == id);
	}

	public SproutRewardData GetWindRewardData(int id)
	{
		return windRewardDataContainer.WindRewardDataList.FirstOrDefault((SproutRewardData x) => x.ID == id);
	}

	public SproutRewardData GetSporeRewardData(int id)
	{
		return sporeRewardDataContainer.SporeRewardDataList.FirstOrDefault((SproutRewardData x) => x.ID == id);
	}

	public SproutRewardData GetShaftRewardData(int id)
	{
		return shaftRewardDataContainer.ShaftRewardDataList.FirstOrDefault((SproutRewardData x) => x.ID == id);
	}

	public BlacklistMemoryData GetBlacklistMemoryData(string id)
	{
		return blacklistDataContainer.BlacklistMemoryDataList.FirstOrDefault((BlacklistMemoryData x) => x.ID == id);
	}

	public PartData GetPartData(string id)
	{
		return playerPartDataContainer.PartDataList.FirstOrDefault((PartData x) => x.ID == id);
	}

	public PartEffectData GetPartEffectData(int id)
	{
		return playerPartDataContainer.PartEffectDataList.FirstOrDefault((PartEffectData x) => x.ID == id);
	}

	public DifficultyStatData GetDifficultyStatData(string id)
	{
		return difficultyDataContainer.DifficultyStatDataList.FirstOrDefault((DifficultyStatData x) => x.ID == id);
	}

	public MapNoticeData GetMapNoticeData(string id)
	{
		return mapNoticeDataContainer.MapNoticeDataList.FirstOrDefault((MapNoticeData x) => x.ID == id);
	}

	public CreditData GetCreditData(int id)
	{
		return creditDataContainer.CreditDataList.FirstOrDefault((CreditData x) => x.ID == id);
	}

	private void OnInitialized()
	{
	}

	public bool IsLocaleExist(string id)
	{
		LocaleData localeData = GetLocaleData(id);
		if (localeData == null)
		{
			localeData = GetItemLocaleData(id);
		}
		if (localeData == null)
		{
			localeData = GetStoreData(id);
		}
		if (localeData == null)
		{
			localeData = GetFieldInteractData(id);
		}
		if (localeData == null)
		{
			localeData = GetMemoryData(id);
		}
		return localeData != null;
	}

	public string GetLocale(string id, Language language = Language.Korean)
	{
		LocaleData localeData = GetLocaleData(id);
		if (localeData == null)
		{
			localeData = GetItemLocaleData(id);
		}
		if (localeData == null)
		{
			localeData = GetStoreData(id);
		}
		if (localeData == null)
		{
			localeData = GetFieldInteractData(id);
		}
		if (localeData == null)
		{
			localeData = GetMemoryData(id);
		}
		if (localeData == null)
		{
			localeData = GetAchievementData(id);
		}
		if (localeData == null)
		{
			Debug.LogError(id + "에 해당하는 Locale 값이 시트에 없습니다. 구글 시트나 스크립터블오브젝트를 확인해주세요.");
			throw new DataException();
		}
		switch (language)
		{
		case Language.Korean:
			return localeData.KO;
		case Language.English:
		{
			string eN = localeData.EN;
			if (!string.IsNullOrEmpty(eN))
			{
				return eN;
			}
			return localeData.KO;
		}
		case Language.Japanese:
		{
			string jA = localeData.JA;
			if (!string.IsNullOrEmpty(jA))
			{
				return jA;
			}
			if (!string.IsNullOrEmpty(localeData.EN))
			{
				return localeData.EN;
			}
			return localeData.KO;
		}
		case Language.TraditionalChinese:
		{
			string zH_HANT = localeData.ZH_HANT;
			if (!string.IsNullOrEmpty(zH_HANT))
			{
				return zH_HANT;
			}
			if (!string.IsNullOrEmpty(localeData.EN))
			{
				return localeData.EN;
			}
			return localeData.KO;
		}
		case Language.SimplifiedChinese:
		{
			string zH_HANS = localeData.ZH_HANS;
			if (!string.IsNullOrEmpty(zH_HANS))
			{
				return zH_HANS;
			}
			if (!string.IsNullOrEmpty(localeData.EN))
			{
				return localeData.EN;
			}
			return localeData.KO;
		}
		case Language.German:
		{
			string dE = localeData.DE;
			if (!string.IsNullOrEmpty(dE))
			{
				return dE;
			}
			if (!string.IsNullOrEmpty(localeData.EN))
			{
				return localeData.EN;
			}
			return localeData.KO;
		}
		case Language.Spanish:
		{
			string eS = localeData.ES;
			if (!string.IsNullOrEmpty(eS))
			{
				return eS;
			}
			if (!string.IsNullOrEmpty(localeData.EN))
			{
				return localeData.EN;
			}
			return localeData.KO;
		}
		case Language.Portuguese:
		{
			string pT = localeData.PT;
			if (!string.IsNullOrEmpty(pT))
			{
				return pT;
			}
			if (!string.IsNullOrEmpty(localeData.EN))
			{
				return localeData.EN;
			}
			return localeData.KO;
		}
		case Language.French:
		{
			string fR = localeData.FR;
			if (!string.IsNullOrEmpty(fR))
			{
				return fR;
			}
			if (!string.IsNullOrEmpty(localeData.EN))
			{
				return localeData.EN;
			}
			return localeData.KO;
		}
		default:
			return null;
		}
	}

	public IReadOnlyList<StoreItemData> GetLeafaItemDataList()
	{
		return storeItemDataContainer.LeafaDataList;
	}

	public IReadOnlyList<StoreItemData> GetJimaItemDataList()
	{
		return storeItemDataContainer.JimaDataList;
	}

	public IReadOnlyList<StoreItemData> GetPwoongItemDataList()
	{
		return storeItemDataContainer.PwoongDataList;
	}

	public IReadOnlyList<StoreItemData> GetCardamineItemDataList()
	{
		return storeItemDataContainer.CardamineDataList;
	}

	public IReadOnlyList<StoreItemData> GetTouristItemDataList()
	{
		return storeItemDataContainer.TouristPotDataList;
	}

	public IReadOnlyList<StoreItemData> GetMuhaItemDataList()
	{
		return storeItemDataContainer.MuhaDataList;
	}

	public IReadOnlyList<StoreItemData> GetGemExchangerItemDataList()
	{
		return storeItemDataContainer.GemExchangerDataList;
	}

	public List<ScenarioData> SearchAllScenarioDataContains(string key)
	{
		return scenarioDataContainer.ScenarioDataList.Where((ScenarioData data) => data.ID.Contains(key)).ToList();
	}

	public IReadOnlyList<LocaleData> GetAllNameLocaleDataList()
	{
		return localeDataContainer.LocaleDataList.Where((LocaleData data) => data.ID.StartsWith("Name_") && !data.ID.EndsWith("_Job")).ToList();
	}

	public IReadOnlyList<TraitData> GetAllTraitDataList()
	{
		return playerStatDataContainer.TraitDataList;
	}

	public IReadOnlyList<EnemyStatData> GetAllEnemyStatDataList()
	{
		return enemyDataContainer.EnemyStatDataList;
	}

	public IReadOnlyList<LocaleData> GetAllFieldInteractDataList()
	{
		return localeDataContainer.FieldInteractDataList;
	}

	public IReadOnlyList<BlacklistMemoryData> GetAllBlacklistMemoryDataList()
	{
		return blacklistDataContainer.BlacklistMemoryDataList;
	}

	public IReadOnlyList<ItemData> GetAllItemDataList()
	{
		return itemDataContainer.ItemDataList;
	}

	public TraitData GetTraitData(string name)
	{
		return playerStatDataContainer.TraitDataList.FirstOrDefault((TraitData x) => x.Name == name);
	}

	public IReadOnlyList<TriggerData> GetAllAchievementTriggerDataList()
	{
		return triggerDataContainer.TriggerDataList.Where((TriggerData data) => data.ID.Contains("_Ach_")).ToList();
	}

	public List<string> GetTipLocaleKeysByProgress(float progress)
	{
		progress *= 100f;
		List<string> list = new List<string>();
		foreach (LocaleData localeData in localeDataContainer.LocaleDataList)
		{
			if (localeData.ID.Contains("Tip_Progress") && float.Parse(localeData.ID.Split('_')[2]) <= progress)
			{
				list.Add(localeData.ID);
			}
		}
		return list;
	}

	public IReadOnlyList<SproutRewardData> GetEarthSproutDataList()
	{
		return earthRewardDataContainer.EarthRewardDataList;
	}

	public IReadOnlyList<SproutRewardData> GetWaterSproutDataList()
	{
		return waterRewardDataContainer.WaterRewardDataList;
	}

	public IReadOnlyList<SproutRewardData> GetFireSproutDataList()
	{
		return fireRewardDataContainer.FireRewardDataList;
	}

	public IReadOnlyList<SproutRewardData> GetWindSproutDataList()
	{
		return windRewardDataContainer.WindRewardDataList;
	}

	public IReadOnlyList<SproutRewardData> GetSporeSproutDataList()
	{
		return sporeRewardDataContainer.SporeRewardDataList;
	}

	public IReadOnlyList<SproutRewardData> GetShaftSproutDataList()
	{
		return shaftRewardDataContainer.ShaftRewardDataList;
	}

	public float GetCurrentDifficultyStat(string id, Difficulty difficulty)
	{
		DifficultyStatData difficultyStatData = GetDifficultyStatData(id);
		return difficulty switch
		{
			Difficulty.None => 1f, 
			Difficulty.Story => difficultyStatData.Story, 
			Difficulty.Easy => difficultyStatData.Easy, 
			Difficulty.Normal => difficultyStatData.Normal, 
			Difficulty.Main => difficultyStatData.Main, 
			_ => throw new ArgumentOutOfRangeException("difficulty", difficulty, null), 
		};
	}

	public EnemyData GetEnemyDataByClearCount(int clearCount, string id)
	{
		return clearCount switch
		{
			0 => GetEnemyData(id), 
			1 => GetEnemy2Data(id), 
			_ => GetEnemy3Data(id), 
		};
	}

	public EnemyStatData GetEnemyStatDataByClearCount(int clearCount, string id)
	{
		return clearCount switch
		{
			0 => GetEnemyStatData(id), 
			1 => GetEnemyStat2Data(id), 
			_ => GetEnemyStat3Data(id), 
		};
	}

	public EnemyMultiplierData GetEnemyMultiplier(int clearCount)
	{
		int num = enemyDataContainer.EnemyMultiplierDataList.Min((EnemyMultiplierData x) => x.ClearCount);
		int num2 = enemyDataContainer.EnemyMultiplierDataList.Max((EnemyMultiplierData x) => x.ClearCount);
		if (num <= clearCount && clearCount <= num2)
		{
			return GetEnemyMultiplierData(clearCount);
		}
		return GetEnemyMultiplierData(Mathf.Clamp(clearCount, num, num2));
	}

	public EnemyDropData GetEnemyDropDataByClearCount(int clearCount, string id)
	{
		return clearCount switch
		{
			0 => GetEnemyDropData(id), 
			1 => GetEnemyDrop2Data(id), 
			_ => GetEnemyDrop3Data(id), 
		};
	}

	public List<LocaleData> GetAllLocaleDataList()
	{
		List<LocaleData> list = new List<LocaleData>();
		list.AddRange(localeDataContainer.LocaleDataList);
		list.AddRange(localeDataContainer.ItemLocaleDataList);
		list.AddRange(localeDataContainer.StoreDataList);
		list.AddRange(localeDataContainer.FieldInteractDataList);
		list.AddRange(localeDataContainer.MemoryDataList);
		list.AddRange(localeDataContainer.AchievementDataList);
		return list;
	}

	public List<ScenarioData> GetAllScenarioDataList()
	{
		return scenarioDataContainer.ScenarioDataList;
	}

	public bool IsCookableIngredient(List<string> acquiredIngredients, string id)
	{
		if (cookableIngredientDictionary == null || recipeIngredientDictionary == null)
		{
			cookableIngredientDictionary = new Dictionary<string, string>();
			recipeIngredientDictionary = new Dictionary<string, List<string>>();
			foreach (StoreItemData touristItemData in GetTouristItemDataList())
			{
				recipeIngredientDictionary.Add(touristItemData.ID, touristItemData.Ingredient);
				foreach (string item in touristItemData.Ingredient)
				{
					cookableIngredientDictionary[item] = touristItemData.ID;
				}
			}
			foreach (StoreItemData leafaItemData in GetLeafaItemDataList())
			{
				recipeIngredientDictionary.Add(leafaItemData.ID, leafaItemData.Ingredient);
				foreach (string item2 in leafaItemData.Ingredient)
				{
					cookableIngredientDictionary[item2] = leafaItemData.ID;
				}
			}
		}
		string valueOrDefault = cookableIngredientDictionary.GetValueOrDefault(id);
		if (string.IsNullOrEmpty(valueOrDefault))
		{
			return false;
		}
		return recipeIngredientDictionary[valueOrDefault].All(acquiredIngredients.Contains);
	}
}
