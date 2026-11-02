using System;
using System.Collections.Generic;
using System.Linq;
using Library;
using SPR.Save;
using UnityEngine;

namespace SPR.UI.Map;

public static class MapCollection
{
	private const string PrefixShadowSprout = "MC_Sprout/";

	private const string PrefixSproutStone = "MC_SproutStone/";

	private const string PrefixStoreNpc = "MC_StoreNPC/";

	private const string PrefixScenarioGroupId = "MC_GroupId/";

	private static readonly Dictionary<FieldSubtitle, List<string>> Dict = new Dictionary<FieldSubtitle, List<string>>();

	private static readonly Dictionary<FieldSubtitle, List<string>> DropItemDict = new Dictionary<FieldSubtitle, List<string>>
	{
		{
			FieldSubtitle.Earthford,
			new List<string> { "CardamineLetter" }
		},
		{
			FieldSubtitle.Library,
			new List<string> { "SealTyche", "SealChasa" }
		},
		{
			FieldSubtitle.WatermirrorBasin,
			new List<string> { "OphilinTreasureKey" }
		},
		{
			FieldSubtitle.MucoraFortress,
			new List<string> { "RoughFlowerPendant" }
		}
	};

	private static readonly Dictionary<ActorName, Dictionary<FieldSubtitle, string>> StoreNpcDict = new Dictionary<ActorName, Dictionary<FieldSubtitle, string>>
	{
		{
			ActorName.Jima,
			new Dictionary<FieldSubtitle, string>
			{
				{
					FieldSubtitle.SilentRuins,
					"Trigger_JimaAtSilentRuins"
				},
				{
					FieldSubtitle.TerradinGarden,
					"Trigger_JimaAtTerradinGarden"
				},
				{
					FieldSubtitle.FadedCastle,
					"Trigger_JimaAtFadedCastle"
				},
				{
					FieldSubtitle.MucoraFortress,
					"Trigger_JimaAtMucoraFortress"
				}
			}
		},
		{
			ActorName.Pwoong,
			null
		},
		{
			ActorName.Cardamine,
			null
		},
		{
			ActorName.Leafa,
			null
		},
		{
			ActorName.TouristPot,
			null
		},
		{
			ActorName.Muha,
			null
		}
	};

	private static readonly Dictionary<ActorName, FieldSubtitle> StoreNpcNotOnIconDict = new Dictionary<ActorName, FieldSubtitle> { 
	{
		ActorName.TouristPot,
		FieldSubtitle.Earthford
	} };

	private static readonly Dictionary<FieldSubtitle, List<string>> ScenarioDict = new Dictionary<FieldSubtitle, List<string>>
	{
		{
			FieldSubtitle.Earthford,
			new List<string> { "Cardamine_GivePendant" }
		},
		{
			FieldSubtitle.AshHills,
			new List<string> { "ChasaCutscene_6_0" }
		},
		{
			FieldSubtitle.Library,
			new List<string> { "Opus_GiveJournal", "VeritaOfficeCutscene_1_0", "Verita_Enhance_First", "Verita_Enhance_Success" }
		},
		{
			FieldSubtitle.MintBureau,
			new List<string> { "Muha_Hello_WS23" }
		},
		{
			FieldSubtitle.UndelCaverns,
			new List<string> { "UC_90_FirstMeet" }
		}
	};

	private static readonly List<(string id, SectorName sector)> UnmatchedSubtitleDict = new List<(string, SectorName)> { ("TerradinGarden_37_CoreStoneStatue", SectorName.TerradinGarden) };

	private static readonly List<(FieldSubtitle field, ActorName npc)> NpcLocations = new List<(FieldSubtitle, ActorName)>();

	private static string GetSproutStoneId(int id)
	{
		return string.Format("{0}{1}", "MC_SproutStone/", id);
	}

	private static string GetStoreItemId(ActorName actor, string id)
	{
		return string.Format("{0}{1}_{2}", "MC_StoreNPC/", actor, id);
	}

	public static void Initialize(IReadOnlyCollection<MapObjectIcon> icons)
	{
		if (!Dict.IsNullOrEmpty())
		{
			return;
		}
		Dict.Clear();
		NpcLocations.Clear();
		FieldSubtitle[] enumValues = Util.GetEnumValues<FieldSubtitle>();
		foreach (FieldSubtitle key in enumValues)
		{
			Dict.Add(key, new List<string>());
		}
		foreach (MapObjectIcon icon in icons)
		{
			FieldSubtitle subtitle = icon.FieldID.GetSubtitle();
			List<string> list = Dict[subtitle];
			if (icon.Type == MapObjectType.FieldItem)
			{
				if (icon.StringValue.Contains("MemoryFragment"))
				{
					continue;
				}
			}
			else
			{
				MapObjectType type = icon.Type;
				if (type != MapObjectType.ItemBox && type != MapObjectType.Relics && type != MapObjectType.CorestoneStatue)
				{
					if (icon.Type == MapObjectType.Sprout)
					{
						list.Add(string.Format("{0}{1}/{2}", "MC_Sprout/", icon.StringValue, icon.IntValue));
					}
					else if (icon.Type == MapObjectType.SproutStone)
					{
						list.AddRange(from rd in Util.GetSproutRewardDataList(icon.FieldID.GetRegionName())
							select GetSproutStoneId(rd.ID));
					}
					else if (icon.Type == MapObjectType.Npc)
					{
						ActorName intValue = (ActorName)icon.IntValue;
						if (StoreNpcDict.TryGetValue(intValue, out var value))
						{
							AddStoreData(intValue, value, subtitle, list);
						}
					}
					continue;
				}
			}
			list.Add(icon.StringValue);
		}
		foreach (KeyValuePair<FieldSubtitle, List<string>> item in DropItemDict)
		{
			Dict[item.Key].AddRange(item.Value);
		}
		foreach (KeyValuePair<FieldSubtitle, List<string>> item2 in ScenarioDict)
		{
			Dict[item2.Key].AddRange(item2.Value.Select((string groupId) => "MC_GroupId/" + groupId));
		}
		foreach (var (actorName2, fieldSubtitle2) in StoreNpcNotOnIconDict)
		{
			AddStoreData(actorName2, null, fieldSubtitle2, Dict[fieldSubtitle2]);
		}
		static void AddStoreData(ActorName actorName3, Dictionary<FieldSubtitle, string> triggers, FieldSubtitle fieldSubtitle3, List<string> targetValue)
		{
			IReadOnlyList<StoreItemData> storeItemDataList = Util.GetStoreItemDataList(actorName3);
			IEnumerable<StoreItemData> source;
			if (triggers != null)
			{
				source = storeItemDataList.Where((StoreItemData data) => data.AvailableTrigger.Equals(triggers[fieldSubtitle3]));
			}
			else
			{
				IEnumerable<StoreItemData> enumerable = storeItemDataList;
				source = enumerable;
			}
			List<string> collection = source.SelectMany((StoreItemData data) => Enumerable.Repeat(GetStoreItemId(actorName3, data.ID), data.Quantity)).ToList();
			targetValue.AddRange(collection);
			NpcLocations.Add((fieldSubtitle3, actorName3));
		}
	}

	public static void GetCollection(FieldSubtitle field, SaveData saveData, out int total, out int current)
	{
		List<string> list = Dict[field];
		List<string> list2 = new List<string>();
		total = list.Count;
		GetCollection_Local(field, saveData, list2, list);
		list2.AddRange(from tuple in NpcLocations.Where(((FieldSubtitle field, ActorName npc) kvp) => kvp.field.Equals(field)).SelectMany(delegate((FieldSubtitle field, ActorName npc) kvp)
			{
				IReadOnlyList<StoreItemData> soldItemIDList = saveData.WorldData.StoreData.GetSoldItemIDList(kvp.npc);
				if (!StoreNpcDict.TryGetValue(kvp.npc, out var value) || value == null || !value.TryGetValue(field, out var trigger))
				{
					return soldItemIDList.Select((StoreItemData data) => (npc: kvp.npc, data: data));
				}
				IEnumerable<string> targetItems = from storeData in Util.GetStoreItemDataList(kvp.npc)
					where storeData.AvailableTrigger.Equals(trigger)
					select storeData.ID;
				return from data in soldItemIDList
					where data.AvailableTrigger.Equals(trigger) && targetItems.Contains(data.ID)
					select (npc: kvp.npc, data: data);
			})
			select GetStoreItemId(tuple.npc, tuple.data.ID));
		list2.AddRange(from groupId in ScenarioDict.Where((KeyValuePair<FieldSubtitle, List<string>> kvp) => kvp.Key.Equals(field)).SelectMany((KeyValuePair<FieldSubtitle, List<string>> kvp) => kvp.Value)
			where saveData.TalkData.GetTalkCount(groupId) > 0
			select groupId);
		current = list2.Count;
	}

	private static void GetCollection_Local(FieldSubtitle field, SaveData saveData, List<string> currents, List<string> ids)
	{
		SectorName sectorName = (SectorName)((int)field >> 3);
		SectorData data;
		bool flag = saveData.WorldData.TryGetSectorData(sectorName, out data);
		bool flag2 = false;
		List<string> value;
		bool flag3 = DropItemDict.TryGetValue(field, out value);
		foreach (string id in ids)
		{
			if (flag && data.TryGetObjectStateNum(id, out var state) && state != 0)
			{
				currents.Add(id);
				continue;
			}
			if (id.StartsWith("MC_Sprout/"))
			{
				string[] array = id.Split('/');
				string id2 = array[1];
				int num = int.Parse(array[2]);
				if (flag && data.TryGetObjectStateNum(id2, out var state2) && state2 >= num)
				{
					currents.Add(id);
					continue;
				}
			}
			if (flag3 && value.Contains(id) && saveData.PlayerData.HasEverObtainedItem(id, out var _))
			{
				currents.Add(id);
			}
			else if (id.StartsWith("MC_SproutStone/"))
			{
				if (!flag2)
				{
					for (int i = 0; i < saveData.PlayerData.GetShadowRewardCount(FieldID.GetRegionName(sectorName)); i++)
					{
						currents.Add(GetSproutStoneId(i + 1));
					}
					flag2 = true;
				}
			}
			else
			{
				(string, SectorName) tuple = UnmatchedSubtitleDict.FirstOrDefault(((string id, SectorName sector) tuple2) => tuple2.id.Equals(id));
				if (!tuple.Equals(default((string, SectorName))) && saveData.WorldData.TryGetObjectState(tuple.Item2, id, out state) && state != 0)
				{
					currents.Add(id);
				}
			}
		}
	}

	public static int GetGlobalCollection(SaveData saveData, out int total, out int current)
	{
		total = 0;
		List<string> list = new List<string>();
		foreach (FieldSubtitle value in Enum.GetValues(typeof(FieldSubtitle)))
		{
			if (value != FieldSubtitle.None)
			{
				List<string> list2 = Dict[value];
				total += list2.Where((string s) => !s.StartsWith("MC_StoreNPC/") && !s.StartsWith("MC_GroupId/")).Count();
				GetCollection_Local(value, saveData, list, list2);
			}
		}
		total += StoreNpcDict.Sum((KeyValuePair<ActorName, Dictionary<FieldSubtitle, string>> kvp) => Util.GetStoreItemDataList(kvp.Key).SelectMany((StoreItemData data) => Enumerable.Repeat(data.ID, data.Quantity)).ToList()
			.Count);
		list.AddRange(NpcLocations.Select(((FieldSubtitle field, ActorName npc) kvp) => kvp.npc).Distinct().SelectMany((ActorName npc) => from data in saveData.WorldData.StoreData.GetSoldItemIDList(npc)
			select GetStoreItemId(npc, data.ID)));
		List<string> list3 = ScenarioDict.SelectMany((KeyValuePair<FieldSubtitle, List<string>> kvp) => kvp.Value).ToList();
		total += list3.Count;
		list.AddRange(list3.Where((string groupId) => saveData.TalkData.GetTalkCount(groupId) > 0));
		current = list.Count;
		return Mathf.Max(current * 100 / total, (current != 0) ? 1 : 0);
	}
}
