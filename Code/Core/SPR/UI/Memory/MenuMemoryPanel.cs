using System;
using System.Collections.Generic;
using System.Linq;
using Library;
using SPR.Achievement;
using SPR.Save;
using SPR.UI.Field;
using SPR.UI.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Memory;

public class MenuMemoryPanel : MenuPanel
{
	[SerializeField]
	private CategoryList categoryList;

	[SerializeField]
	private ScrollBoard scrollBoard;

	[SerializeField]
	private MemoryDetailBoard memoryDetailBoard;

	[SerializeField]
	private RectTransform slotParent;

	private readonly Dictionary<MemoryCategory, List<MemoryData>> memoryData = new Dictionary<MemoryCategory, List<MemoryData>>();

	private IEnumerable<string> actorsCache;

	private IEnumerable<string> enemyDataCache;

	private IEnumerable<string> interactDataCache;

	private IEnumerable<string> recordIdBlacklist;

	private readonly List<MemoryDataCell> allCells = new List<MemoryDataCell>();

	private readonly Dictionary<MemoryCategory, int> lastSelectedIndex = new Dictionary<MemoryCategory, int>
	{
		{
			MemoryCategory.Character,
			0
		},
		{
			MemoryCategory.Boss,
			0
		},
		{
			MemoryCategory.Creature,
			0
		},
		{
			MemoryCategory.Record,
			0
		}
	};

	private MemoryCategory currentCategory;

	private int currentIndex;

	private int currentLength;

	private float spacing;

	public override MenuCategory Category => MenuCategory.Memory;

	protected override bool CanChangeCategory => !MenuPanel.IsRest;

	private bool IsFocusingCategory => lastSelectedIndex[currentCategory] == -1;

	private static ObjectPoolManager ObjectPoolManager => Game.Instance.ObjectPoolManager;

	private static SaveData SaveData => Game.Instance.SaveData;

	private static WanderingData WanderingData => SaveData.WanderingData;

	private static MessageBoard MessageBoard => Game.Instance.UIManager.FieldCanvas.MessageBoard;

	public override void Initialize()
	{
		MemoryCategory[] enumValues = Util.GetEnumValues<MemoryCategory>();
		for (int i = 0; i < enumValues.Length; i++)
		{
			AddressableHelper.PreloadAssetsAsync<Sprite>(GetLabel(enumValues[i])).Forget();
		}
		CacheData();
		categoryList.Initialize();
		spacing = slotParent.GetComponent<VerticalLayoutGroup>().spacing;
		UpdateMemoryData();
	}

	protected override void OnActivate()
	{
		CheckAchievementCleared();
		FocusCategory(currentCategory, force: true);
	}

	protected override void OnControl()
	{
		int subCategory = MenuPanel.InputManager.subCategory;
		int horizontalDownUI = MenuPanel.InputManager.horizontalDownUI;
		int num = (IsFocusingCategory ? Util.GetPrioritizedControl(subCategory, horizontalDownUI) : Util.GetNotOverlappedControl(subCategory, horizontalDownUI));
		if (num != 0)
		{
			MemoryCategory memoryCategory = Util.AddToEnumCircular(currentCategory, num);
			lastSelectedIndex[currentCategory] = -1;
			lastSelectedIndex[memoryCategory] = ((subCategory == 0) ? (-1) : 0);
			FocusCategory(memoryCategory);
		}
		int verticalDownUI = MenuPanel.InputManager.verticalDownUI;
		if (verticalDownUI != 0)
		{
			int num2 = lastSelectedIndex[currentCategory];
			int num3 = Mathf.Clamp(num2 - verticalDownUI, -1, allCells.Count - 1);
			if (num2 == num3)
			{
				return;
			}
			MoveCursor(num3);
		}
		if ((MenuPanel.InputManager.menuDown || MenuPanel.InputManager.cancelDown) && !IsFocusingCategory)
		{
			MoveCursor(-1);
		}
	}

	protected override void OnDeactivate()
	{
		ClearChildren();
		foreach (MemoryCategory item in lastSelectedIndex.Select((KeyValuePair<MemoryCategory, int> kvp) => kvp.Key).ToList())
		{
			lastSelectedIndex[item] = 0;
		}
	}

	private void CacheData()
	{
		recordIdBlacklist = from data in DataManager.Instance.GetAllBlacklistMemoryDataList()
			select data.ID;
		actorsCache = from data in DataManager.Instance.GetAllNameLocaleDataList()
			select data.ID.Remove(0, 5);
		enemyDataCache = from data in DataManager.Instance.GetAllEnemyStatDataList()
			select data.ID into id
			where !recordIdBlacklist.Contains(id)
			select id;
		interactDataCache = from data in DataManager.Instance.GetAllFieldInteractDataList()
			where data.ID.Contains("_Desc")
			select data.ID.Remove(data.ID.Length - 5, 5) into id
			where !recordIdBlacklist.Contains(id)
			select id;
	}

	private void FocusCategory(MemoryCategory category, bool force = false)
	{
		if (force || currentCategory != category)
		{
			if (!force)
			{
				MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Sub_Move");
			}
			categoryList.Focus((int)category, !force);
			ClearAndCreateCells(category);
			if (allCells.FirstOrDefault().IsNull())
			{
				lastSelectedIndex[category] = -1;
				FocusCell(null);
			}
			else
			{
				lastSelectedIndex[category] = Mathf.Clamp(lastSelectedIndex[category], -1, allCells.Count - 1);
				FocusCell(allCells.GetElementSafely(lastSelectedIndex[category]));
			}
			currentCategory = category;
			UpdateCategoryCursor();
		}
	}

	private void MoveCursor(int newIndex)
	{
		lastSelectedIndex[currentCategory] = newIndex;
		FocusCell(allCells.GetElementSafely(newIndex), notInstant: true);
	}

	private void FocusCell(MemoryDataCell targetCell, bool notInstant = false)
	{
		if (targetCell.IsNull())
		{
			DefocusAllCells();
			UpdateCategoryCursor();
			memoryDetailBoard.SetData(default(MemoryData));
			MemoryDataCell memoryDataCell = allCells.First();
			if (!memoryDataCell.IsNull())
			{
				Scroll(memoryDataCell);
			}
			if (notInstant)
			{
				MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
			}
		}
		else
		{
			categoryList.SetCursor(-1);
			Scroll(targetCell);
			DefocusAllCells();
			targetCell.Focus(value: true);
			if (notInstant)
			{
				MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
			}
			memoryDetailBoard.SetData(targetCell.MemoryData);
		}
		void Scroll(MemoryDataCell memoryDataCell2)
		{
			scrollBoard.ScrollIfTargetIsOutOfViewport(memoryDataCell2.RectTransform, memoryDataCell2.RectTransform.sizeDelta.y + spacing, !notInstant);
		}
	}

	private void UpdateCategoryCursor()
	{
		categoryList.SetCursor((int)(IsFocusingCategory ? currentCategory : ((MemoryCategory)(-1))));
	}

	private void DefocusAllCells()
	{
		foreach (MemoryDataCell allCell in allCells)
		{
			allCell.Focus(value: false);
		}
	}

	private void ClearAndCreateCells(MemoryCategory category)
	{
		ClearChildren();
		List<MemoryData> memoryDataByCategory = GetMemoryDataByCategory(category);
		CreateMemoryList(category, memoryDataByCategory);
	}

	private void ClearChildren()
	{
		allCells.Where((MemoryDataCell cell) => !cell.IsNull()).ForEach(delegate(MemoryDataCell cell)
		{
			cell.ReturnToPool();
		});
		foreach (Transform item in slotParent.Cast<Transform>().ToList())
		{
			MemoryAreaTitle component2;
			if (item.TryGetComponent<MemoryDataCell>(out var component))
			{
				component.ReturnToPool();
			}
			else if (item.TryGetComponent<MemoryAreaTitle>(out component2))
			{
				component2.ReturnToPool();
			}
			else
			{
				UnityEngine.Object.Destroy(item.gameObject);
			}
		}
		allCells.Clear();
	}

	private List<MemoryData> GetMemoryDataByCategory(MemoryCategory category)
	{
		UpdateMemoryData();
		return memoryData[category];
	}

	private void UpdateMemoryData()
	{
		memoryData.Clear();
		UpdateCharacterMemoryData();
		UpdateBossAndCreatureMemoryData();
		UpdateRecordMemoryData();
	}

	private void UpdateCharacterMemoryData()
	{
		TalkData talkData = SaveData.TalkData;
		List<MemoryData> list = new List<MemoryData>();
		foreach (string item in actorsCache)
		{
			if (Enum.TryParse<ActorName>(item, out var result) && !IsInvalidActor(result))
			{
				MemoryLevel characterMemoryLevel = GetCharacterMemoryLevel(result, talkData);
				list.Add(new MemoryData(MemoryCategory.Character, result.ToString(), characterMemoryLevel));
			}
		}
		memoryData.Add(MemoryCategory.Character, list);
	}

	private static MemoryLevel GetCharacterMemoryLevel(ActorName actor, TalkData talkData)
	{
		if (!IsMemoryKnown(actor, talkData))
		{
			return MemoryLevel.None;
		}
		if (!IsMemoryCleared(actor))
		{
			return MemoryLevel.Know;
		}
		return MemoryLevel.Clear;
	}

	private void UpdateBossAndCreatureMemoryData()
	{
		EnemyFightData enemyFightData = SaveData.EnemyFightData;
		List<MemoryData> list = new List<MemoryData>();
		List<MemoryData> list2 = new List<MemoryData>();
		foreach (string item in enemyDataCache)
		{
			if (Enum.TryParse<BossName>(item, out var result))
			{
				MemoryLevel level = ((enemyFightData.GetBossTryCount(result) > 0) ? ((result == BossName.PrimordialFlame || !enemyFightData.IsBossCleared(result)) ? MemoryLevel.Know : MemoryLevel.Clear) : MemoryLevel.None);
				list.Add(new MemoryData(MemoryCategory.Boss, item, level));
			}
			else
			{
				if (item.Equals("HiddenMimic"))
				{
					continue;
				}
				if (item.Equals("ImahenCoreMachine"))
				{
					SectorData data;
					int state;
					MemoryLevel level2 = ((SaveData.WorldData.TryGetSectorData(SectorName.SolinLab, out data) && data.TryGetObjectStateNum("MorphedImahen", out state) && state == 4) ? MemoryLevel.Clear : ((enemyFightData.GetBossTryCount(BossName.Imahen) > 0) ? MemoryLevel.Know : MemoryLevel.None));
					list.Add(new MemoryData(MemoryCategory.Boss, BossName.Imahen.ToString(), level2));
					MemoryLevel memoryLevel = ((enemyFightData.GetBossTryCount(BossName.Imahen) > 0) ? MemoryLevel.Know : MemoryLevel.None);
					list2.Add(new MemoryData(MemoryCategory.Creature, item, memoryLevel));
					if (memoryLevel != MemoryLevel.None)
					{
						UpdateMemory(MemoryCategory.Creature, item, resetByClear: false, checkAchievement: false);
					}
				}
				else
				{
					MemoryLevel level3 = ((enemyFightData.GetCreatureDeathCount(item) > 0 || enemyFightData.GetCreatureKillCount(item) > 0) ? MemoryLevel.Know : MemoryLevel.None);
					list2.Add(new MemoryData(MemoryCategory.Creature, item, level3));
				}
			}
		}
		memoryData.Add(MemoryCategory.Boss, list);
		memoryData.Add(MemoryCategory.Creature, list2);
	}

	private void UpdateRecordMemoryData()
	{
		WorldData worldData = SaveData.WorldData;
		List<MemoryData> list = new List<MemoryData>();
		foreach (string item in interactDataCache)
		{
			MemoryLevel level = (worldData.HasInteractedFieldObject(item) ? MemoryLevel.Know : MemoryLevel.None);
			list.Add(new MemoryData(MemoryCategory.Record, item, level));
		}
		memoryData.Add(MemoryCategory.Record, list);
	}

	private void CreateMemoryList(MemoryCategory category, List<MemoryData> memoryDataList)
	{
		SectorName sectorName = SectorName.None;
		SectorName[] enumValues = Util.GetEnumValues<SectorName>();
		foreach (MemoryData memoryData in memoryDataList)
		{
			if (category == MemoryCategory.Record)
			{
				SectorName sectorName2 = sectorName;
				if (!memoryData.ID.Contains(sectorName2.ToString()))
				{
					if (memoryData.ID.Contains("HeatCoreLV0"))
					{
						sectorName = SectorName.SilentRuins;
						MemoryAreaTitle memoryAreaTitle = ObjectPoolManager.GetObject<MemoryAreaTitle>("Memory Area Title.prefab", slotParent);
						memoryAreaTitle.transform.localScale = Vector3.one;
						memoryAreaTitle.SetData(sectorName);
					}
					else
					{
						SectorName[] array = enumValues;
						for (int i = 0; i < array.Length; i++)
						{
							SectorName sectorName3 = array[i];
							if (memoryData.ID.Contains(sectorName3.ToString()) && sectorName3 != sectorName)
							{
								sectorName = sectorName3;
								MemoryAreaTitle memoryAreaTitle2 = ObjectPoolManager.GetObject<MemoryAreaTitle>("Memory Area Title.prefab", slotParent);
								memoryAreaTitle2.transform.localScale = Vector3.one;
								memoryAreaTitle2.SetData(sectorName);
								break;
							}
						}
					}
				}
			}
			MemoryDataCell memoryDataCell = ObjectPoolManager.GetObject<MemoryDataCell>("Memory Data Cell.prefab", slotParent);
			memoryDataCell.transform.localScale = Vector3.one;
			memoryDataCell.SetData(memoryData);
			allCells.Add(memoryDataCell);
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(slotParent);
	}

	public static string GetTitleID(MemoryData md)
	{
		return md.Category switch
		{
			MemoryCategory.Character => "Name_" + md.ID, 
			MemoryCategory.Boss => "Boss_" + md.ID, 
			MemoryCategory.Creature => "Creature_" + md.ID, 
			MemoryCategory.Record => GetRecordId(md.ID), 
			_ => "", 
		};
		static string GetRecordId(string id)
		{
			string text = id + "_Name";
			if (!DataManager.Instance.IsLocaleExist(text))
			{
				return "Memory_" + id + "_Name";
			}
			return text;
		}
	}

	public static string GetLabel(MemoryCategory category)
	{
		return $"Portrait_{category}";
	}

	public void TryUpdateClearedMemory(bool wasCleared, ActorName actorName)
	{
		if (!wasCleared && IsMemoryCleared(actorName))
		{
			UpdateMemory(actorName, isClear: true, notShowUnknown: true);
		}
	}

	public void UpdateMemory(ActorName actorName, bool isClear, bool notShowUnknown = false)
	{
		if (!IsInvalidActor(actorName) && (!notShowUnknown || GetCharacterMemoryLevel(actorName, SaveData.TalkData) != MemoryLevel.None))
		{
			MessageBoard.Activate(actorName);
			UpdateMemory(MemoryCategory.Character, actorName.ToString(), isClear);
		}
	}

	public void UpdateMemory(BossName bossName, bool isClear, bool showBoard = true)
	{
		if (showBoard)
		{
			MessageBoard.Activate(bossName);
		}
		UpdateMemory(MemoryCategory.Boss, bossName.ToString(), isClear);
	}

	public void UpdateMemoryCreature(string id)
	{
		if (!recordIdBlacklist.Contains(id))
		{
			UpdateMemory(MemoryCategory.Creature, id, resetByClear: false);
		}
	}

	public void UpdateMemoryRecord(string id)
	{
		if (!recordIdBlacklist.Contains(id))
		{
			UpdateMemory(MemoryCategory.Record, id, resetByClear: false);
		}
	}

	private void UpdateMemory(MemoryCategory category, string id, bool resetByClear, bool checkAchievement = true)
	{
		WanderingData.AddUnwatchedMemory(category, id, resetByClear);
		if (checkAchievement)
		{
			CheckAchievementCleared();
		}
	}

	public void ShowCurrentData(MemoryCategory category, string id)
	{
		currentCategory = category;
		if (!this.memoryData.TryGetValue(category, out var value))
		{
			return;
		}
		foreach (var (memoryData, value2) in value.Select((MemoryData v, int i) => (v: v, i: i)))
		{
			if (id.Equals(memoryData.ID))
			{
				lastSelectedIndex[category] = value2;
			}
		}
	}

	public static bool IsMemoryKnown(ActorName actorName, TalkData talkData)
	{
		return actorName switch
		{
			ActorName.Haechi => WanderingData.IsWatchedCutscene("HaechiRescueCutsceneProducer", 1), 
			ActorName.Arte => talkData.GetInteractCount(actorName) > 0 || talkData.GetTalkCount("Muha_Bye") > 0, 
			_ => talkData.GetInteractCount(actorName) > 0, 
		};
	}

	public static bool IsMemoryCleared(ActorName actorName)
	{
		int current;
		return actorName switch
		{
			ActorName.Chasa => SaveData.EnemyFightData.IsBossCleared(BossName.Chasa), 
			ActorName.Jima => WatchedTalk("Jima_SoldOutPerfectly"), 
			ActorName.Lunya => WatchedTalk("Lunya_Talk_WentMucoraFortress_1") && WatchedTalk("Lunya_Library_Verita_Cleared"), 
			ActorName.Cor => WatchedTalk("Earthford_28_Cor"), 
			ActorName.Flutis => WatchedTalk("Flutis_SoldOut") && WatchedTalk("Flutis_Talk_5"), 
			ActorName.Opus => WatchedTalk("Opus_GiveJournal") && WatchedTalk("Opus_GiveLetter") && WatchedTalk("Opus_Talk_8"), 
			ActorName.Cardamine => SaveData.PlayerData.HasEverObtainedItem("CardamineLetter", out current), 
			ActorName.Pwoong => WatchedTalk("Pwoong_Talk_7") && WatchedTalk("Pwoong_VeritaInfected"), 
			ActorName.Myoso => WatchedTalk("SilentRuins_09_Myoso") || WatchedTalk("BaranCutscene_9_5"), 
			ActorName.Muha => WatchedTalk("Muha_Bye"), 
			ActorName.Verita => WatchedTalk("VeritaGateCutscene_0_0"), 
			ActorName.Leafa => WatchedTalk("Leafa_Talk_VeritaCleared_3") && WatchedTalk("Leafa_SoldOut"), 
			ActorName.Haechi => WatchedTalk("VeritaGateCutscene_0_0"), 
			ActorName.Imahen => SaveData.WanderingData.IsWatchedCutscene("ImahenLabCutsceneProducer"), 
			_ => false, 
		};
	}

	private static bool WatchedTalk(string groupId)
	{
		return SaveData.TalkData.GetTalkCount(groupId) > 0;
	}

	public static bool IsInvalidActor(ActorName actor)
	{
		if (actor != ActorName.None)
		{
			return actor == ActorName.TouristPot;
		}
		return true;
	}

	private void CheckAchievementCleared()
	{
		UpdateMemoryData();
		if (memoryData.SelectMany((KeyValuePair<MemoryCategory, List<MemoryData>> kvp) => kvp.Value).ToList().All(IsFullLevel))
		{
			AchievementHelper.Unlock("CollectAllMemories");
		}
		static bool IsFullLevel(MemoryData data)
		{
			if (!data.ID.Equals("Chasa") || Game.Instance.SaveData.TalkData.GetTalkCount("ChasaRiverCutscene_2_1") != 0)
			{
				bool flag;
				switch (data.Category)
				{
				case MemoryCategory.Character:
					flag = data.Level == (MemoryLevel)((data.ID == ActorName.Arte.ToString() || data.ID == ActorName.WaterDragon.ToString()) ? 1 : 2);
					break;
				case MemoryCategory.Boss:
					flag = data.Level == (MemoryLevel)((data.ID == BossName.PrimordialFlame.ToString()) ? 1 : 2);
					break;
				case MemoryCategory.Creature:
				case MemoryCategory.Record:
					flag = data.Level == MemoryLevel.Know;
					break;
				default:
					flag = false;
					break;
				}
				bool num = flag;
				if (!num)
				{
					DebugLog.AchievementLog($"{data.ID}({data.Category}) : {data.Level} 가 false입니다!");
				}
				return num;
			}
			return false;
		}
	}
}
