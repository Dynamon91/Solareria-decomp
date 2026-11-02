using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SPR.UI.Store;
using StaticData;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class SaveData
{
	[SerializeField]
	private PlayerData playerData;

	[SerializeField]
	private TalkData talkData;

	[SerializeField]
	private WorldData worldData;

	[SerializeField]
	private EnemyFightData enemyFightData;

	[SerializeField]
	private WanderingData wanderingData;

	[SerializeField]
	private float playtimeSec;

	[SerializeField]
	private FieldID lastFieldID;

	[SerializeField]
	private Difficulty difficulty;

	[SerializeField]
	private Difficulty easiestDifficulty = Difficulty.Main;

	[SerializeField]
	private int clearCount;

	[SerializeField]
	private long lastDateTimeAsLong;

	private float recordStartTime;

	public PlayerData PlayerData => playerData;

	public TalkData TalkData => talkData;

	public WorldData WorldData => worldData;

	public EnemyFightData EnemyFightData => enemyFightData;

	public WanderingData WanderingData => wanderingData;

	public bool IsNewSaveData => lastFieldID == FieldID.NewGameStartSceneID();

	public FieldID LastFieldID => lastFieldID;

	public Difficulty Difficulty => difficulty;

	public Difficulty EasiestDifficulty => easiestDifficulty;

	public int ClearCount => clearCount;

	public string LastDateTime => new DateTime(lastDateTimeAsLong, DateTimeKind.Utc).ToLocalTime().ToString("yyyy.MM.dd. HH:mm");

	public SaveData()
	{
		Reset();
	}

	public bool IsValid()
	{
		FixCharonCutscene();
		FixMuhaByeWarmhornShell();
		FixMoldinClearVeritaCutscene();
		FixSolinLab03UnnecessaryCutscenePrefab();
		FixJimaSoldData();
		FixAshHillsFiveRelicsInfoStoneNoti();
		FixImahenBossCutscene();
		FixDropData();
		FixEnhancedWristbandNum();
		FixEnhancedWristbandIngredient();
		FixCorestoneDefenseDownRolling();
		FixAbandonedTunnelsAltar();
		return true;
		void ExchangeCardData(int remove, int add)
		{
			PlayerData.RemoveCardData(remove, WanderingData);
			PlayerData.AddCardData_Internal(add, WanderingData);
		}
		void FixAbandonedTunnelsAltar()
		{
			if (LastFieldID == new FieldID(SectorName.AbandonedTunnels, 8))
			{
				lastFieldID = new FieldID(SectorName.AbandonedTunnels, 5);
			}
		}
		void FixAshHillsFiveRelicsInfoStoneNoti()
		{
			WanderingData.RemoveUnwatchedInfoStoneIcon("AshHillsFiveRelics");
		}
		void FixCharonCutscene()
		{
			if (WorldData.TryGetSectorData(SectorName.SilentRuins, out var data) && data.TryGetObjectStateNum("CharonCutsceneProducer", out var state) && state == 4)
			{
				data.SaveObjectState("CharonCutsceneProducer", 5);
			}
		}
		void FixCorestoneDefenseDownRolling()
		{
			SectorData data;
			SectorData.ObjectState objectState;
			bool flag = WorldData.TryGetSectorData(SectorName.SilentRuins, out data) && data.TryGetObjectState("SilentRuins_04_CoreStoneStatue", out objectState) && objectState.state == 1;
			IReadOnlyList<StoreItemData> soldItemIDList = WorldData.StoreData.GetSoldItemIDList(StoreType.Artifacts);
			StoreItemData downRollingSoldData = soldItemIDList?.FirstOrDefault((StoreItemData storeItemData) => storeItemData.ID.Equals("DownRolling"));
			bool flag2 = downRollingSoldData != null;
			bool flag3 = soldItemIDList?.FirstOrDefault((StoreItemData storeItemData) => storeItemData.ID.Equals("Defense")) != null;
			if (flag2 && flag3)
			{
				DebugLog.GeneralLogError("카다민이 낙법 코어 스톤과 데미지 감소 코어스톤을 둘 다 판매한 것으로 기록된 데이터입니다!");
				throw new Exception();
			}
			if (!(flag && flag3))
			{
				if (flag && flag2)
				{
					ExchangeSoldData();
				}
				else if (flag || flag3 || flag2)
				{
					if (flag)
					{
						ExchangeCardData(5, 1001);
					}
					else if (flag2)
					{
						ExchangeSoldData();
						ExchangeCardData(1001, 5);
					}
				}
			}
			void ExchangeSoldData()
			{
				StoreItemData data2 = Util.GetStoreItemDataList(ActorName.Cardamine).FirstOrDefault((StoreItemData storeItemData) => storeItemData.ID.Equals("Defense"));
				WorldData.StoreData.RemoveSoldItemData(StoreType.Artifacts, new List<StoreItemData> { downRollingSoldData });
				WorldData.StoreData.AddSoldItem(StoreType.Artifacts, data2);
			}
		}
		void FixDropData()
		{
			PlayerData.DropData currentDropData = PlayerData.CurrentDropData;
			FieldID fieldID = currentDropData.FieldID;
			if (fieldID.SectorName == SectorName.SolinLab && fieldID.SceneIndex == 98 && currentDropData.WorldPos.y < -14.5f)
			{
				Vector2 vector = new Vector2(currentDropData.WorldPos.x, -14.5f);
				AreaBox areaBox = new AreaBox(new Vector2Int(-110, -20), new Vector2Int(480, 24));
				currentDropData.ChangePos(vector, SoulDropInfo.GetAreaNormalizedPosition(areaBox, vector));
				DebugLog.GeneralLog("Validating Drop Data of SolinLab_98");
			}
		}
		void FixEnhancedWristbandIngredient()
		{
			if (PlayerData.GetAcquiredItemCount("EnhancedWristband") <= 0)
			{
				return;
			}
			foreach (string relicSpirit in ItemDataKey.RelicSpirits)
			{
				if (PlayerData.TryRemoveAcquiredItem(relicSpirit, 1, checkInventory: false))
				{
					WanderingData.RemoveUnwatchedInventoryItem(relicSpirit);
				}
			}
		}
		void FixEnhancedWristbandNum()
		{
			int acquiredItemCount = PlayerData.GetAcquiredItemCount("EnhancedWristband");
			if (acquiredItemCount > 1)
			{
				PlayerData.TryRemoveAcquiredItem("EnhancedWristband", acquiredItemCount - 1);
			}
		}
		void FixImahenBossCutscene()
		{
			if (EnemyFightData.IsBossCleared(BossName.Imahen) && WorldData.TryGetSectorData(SectorName.SolinLab, out var data) && data.TryGetObjectStateNum("ImahenBossCutsceneProducer", out var state) && state == 2)
			{
				data.SaveObjectState("ImahenBossCutsceneProducer", 3);
				DebugLog.GeneralLog("Validating CutsceneData of ImahenBossCutsceneProducer : 2 -> 3");
			}
		}
		void FixJimaSoldData()
		{
			IReadOnlyList<StoreItemData> soldItemIDList = WorldData.StoreData.GetSoldItemIDList(ActorName.Jima);
			IReadOnlyList<StoreItemData> storeItemDataList = Util.GetStoreItemDataList(ActorName.Jima);
			List<StoreItemData> list = new List<StoreItemData>();
			foreach (StoreItemData store in storeItemDataList)
			{
				list.AddRange(soldItemIDList.Where((StoreItemData data) => data.ID == store.ID && data.AvailableTrigger == store.AvailableTrigger && data.Quantity > store.Quantity));
			}
			WorldData.StoreData.RemoveSoldItemData(StoreType.Peddler, list);
		}
		void FixMoldinClearVeritaCutscene()
		{
			if (WanderingData.IsWatchedCutscene("MoldinClearVeritaCutsceneProducer"))
			{
				string text = "MoldinClearVeritaEntranceCutsceneProducer";
				string text2 = "MoldinClearVeritaOfficeCutsceneProducer";
				string text3 = "MoldinClearVeritaPortalCutsceneProducer";
				WanderingData.RemoveWatchedCutscene("MoldinClearVeritaCutsceneProducer");
				WanderingData.AddWatchedCutscene(text, 1);
				WanderingData.AddWatchedCutscene(text2, 1);
				WanderingData.AddWatchedCutscene(text3, 1);
				TalkData.RemoveTalk("MoldinClearVeritaCutscene_1_0");
				TalkData.RemoveTalk("MoldinClearVeritaCutscene_1_1");
				TalkData.AddTalk("MoldinClearVeritaOffice_1_0");
				TalkData.AddTalk("MoldinClearVeritaOffice_1_1");
				if (WorldData.TryGetSectorData(SectorName.AshHills, out var data))
				{
					data.RemoveObjectState("MoldinClearVeritaCutsceneProducer");
					data.SaveObjectState(text, 2);
				}
				if (WorldData.TryGetSectorData(SectorName.Library, out var data2))
				{
					data2.RemoveObjectState("MoldinClearVeritaCutsceneProducer");
					data2.SaveObjectState(text2, 2);
					data2.SaveObjectState(text3, 3);
				}
			}
		}
		void FixMuhaByeWarmhornShell()
		{
			if (TalkData.GetTalkCount("Muha_Bye") > 0 && TalkData.GetTalkCount("Muha_Hello_WS23") == 0)
			{
				TalkData.AddTalk("Muha_Hello_WS23");
				PlayerData.AddAcquiredItem_Internal("EmptyWarmhornShell", WanderingData);
			}
		}
		void FixSolinLab03UnnecessaryCutscenePrefab()
		{
			if (WorldData.TryGetSectorData(SectorName.SolinLab, out var data))
			{
				data.RemoveObjectState("MoldinClearVeritaCutsceneProducer");
			}
		}
	}

	public void SetLastBonfireScene(FieldID fieldID)
	{
		lastFieldID = fieldID;
	}

	public void Reset()
	{
		playerData = new PlayerData();
		worldData = new WorldData();
		talkData = new TalkData();
		enemyFightData = new EnemyFightData();
		wanderingData = new WanderingData();
		lastFieldID = new FieldID(SectorName.None, 0);
		playtimeSec = 0f;
		difficulty = Difficulty.None;
		clearCount = 0;
		lastDateTimeAsLong = new DateTime(0L, DateTimeKind.Utc).Ticks;
	}

	public int GetCurrentPlayTime()
	{
		return Mathf.RoundToInt(playtimeSec);
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(lastFieldID);
		stringBuilder.AppendLine();
		stringBuilder.Append(Mathf.RoundToInt(playtimeSec));
		return stringBuilder.ToString();
	}

	public void StartRecordPlayTime()
	{
		recordStartTime = Time.realtimeSinceStartup;
	}

	public void RecordPlayTime()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		float num = realtimeSinceStartup - recordStartTime;
		if (!(recordStartTime < 0f) && !(num < 0f))
		{
			playtimeSec += num;
			recordStartTime = realtimeSinceStartup;
		}
	}

	public void RecordDateTime()
	{
		lastDateTimeAsLong = DateTime.UtcNow.Ticks;
	}

	public float GetGameProgress()
	{
		int num = EnemyFightData.BossClearCount();
		float num2 = Enum.GetValues(typeof(BossName)).Length;
		return (float)num / num2;
	}

	public void SetDifficulty(Difficulty newDifficulty)
	{
		difficulty = newDifficulty;
		if (easiestDifficulty > newDifficulty)
		{
			easiestDifficulty = newDifficulty;
		}
	}

	public void AddClearCount()
	{
		clearCount++;
	}
}
