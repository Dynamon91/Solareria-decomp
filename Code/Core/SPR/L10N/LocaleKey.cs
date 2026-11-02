using SPR.Field;
using SPR.UI;
using SPR.UI.Map;
using SPR.UI.Trait;
using SPR.UI.Tutorial;
using SPR.UI.Upgrade;
using StaticData;

namespace SPR.L10N;

public static class LocaleKey
{
	public const string StorePurchaseMessage = "UI_Store_Message_Purchased";

	public const string StoreExchangeMessage = "UI_Store_Message_Exchanged";

	public const string ConfirmPopupYes = "UI_ConfirmPopup_Yes";

	public const string ConfirmPopupNo = "UI_ConfirmPopup_No";

	public const string EngravingDescFormat = "UI_Upgrade_Engraving_DescFormat";

	public const string EngravingDescExtra = "UI_Upgrade_Engraving_DescExtra";

	public const string EngravingDescNone = "UI_Upgrade_Engraving_DescNone";

	public const string LibraryFrontWarning = "UI_Library_Front_Warning";

	public const string WarmthTunnelWarning = "UI_Warmth_Tunnel_Warning";

	public const string TeleportUnableAreaWarning = "UI_Teleport_Warning_UnableArea";

	public const string WetWarning = "UI_Wet_Warning";

	public const string MapGlobalCollectionFormat = "UI_Map_GlobalCollection_Format";

	public const string GlobalCollectionName = "UI_GlobalCollection_Name";

	public const string SolinLabEntranceWarning = "UI_SolinLabEntranceDoor_Warning";

	public const string CoreStoneUpgradeAvailableAtAltarWarning = "UI_CoreStone_Warning_UpgradeAvailableAtAltar";

	public const string MemoryChasaClear = "Memory_Boss_Chasa_Clear";

	public const string MemoryChasaTruth = "Memory_Chasa_Clear_Truth";

	public const string HeatCoreLV0 = "HeatCoreLV0";

	public const string KeyGuideBuy = "UI_KeyGuide_Buy";

	public const string KeyGuideCook = "UI_KeyGuide_Cook";

	public static string SectorName(SectorName sectorName, bool desc = false)
	{
		string text = (desc ? "_Desc" : string.Empty);
		if (sectorName == SPR.SectorName.Library)
		{
			return "Subtitle_Library" + text;
		}
		return $"Sector_{sectorName}{text}";
	}

	public static string AltarName(FieldID fieldID)
	{
		return "Map_Altar_" + fieldID.ToString();
	}

	public static string ActorName(string actorName)
	{
		return "Name_" + actorName;
	}

	public static string ItemCategory(ItemType itemType)
	{
		return $"Inventory_{itemType}";
	}

	public static string ItemName(string itemID)
	{
		ItemType itemType = Game.DataManager.GetItemData(itemID).ItemType;
		return $"{itemType}_{itemID}_Name";
	}

	public static string ItemName(ItemType itemType, string itemID)
	{
		if (itemType == ItemType.None)
		{
			return itemID + "_Name";
		}
		return $"{itemType}_{itemID}_Name";
	}

	public static string ItemDesc(string itemID)
	{
		ItemType itemType = Game.DataManager.GetItemData(itemID).ItemType;
		return $"{itemType}_{itemID}_Desc";
	}

	public static string ItemDesc(ItemType itemType, string itemID)
	{
		if (itemType == ItemType.None)
		{
			return itemID + "_Desc";
		}
		return $"{itemType}_{itemID}_Desc";
	}

	public static string ItemDescStore(string itemID)
	{
		ItemType itemType = Game.DataManager.GetItemData(itemID).ItemType;
		string text = $"{itemType}_{itemID}_Desc_Store";
		if (!Game.DataManager.IsLocaleExist(text))
		{
			return ItemDesc(itemID);
		}
		return text;
	}

	public static string ItemEffect(string itemID)
	{
		ItemType itemType = Game.DataManager.GetItemData(itemID).ItemType;
		if (itemType == ItemType.Food || itemType == ItemType.Material || itemType == ItemType.CoreStone || itemType == ItemType.Part)
		{
			return $"{itemType}_{itemID}_Effect";
		}
		return $"{itemType}_Effect";
	}

	public static string ItemFlavorText(string itemID, ActorName seller)
	{
		ItemType itemType = Game.DataManager.GetItemData(itemID).ItemType;
		return $"{itemType}_{itemID}_Flavor_{seller}";
	}

	public static string ItemMapInfo(string itemID)
	{
		ItemType itemType = Game.DataManager.GetItemData(itemID).ItemType;
		return $"{itemType}_{itemID}_MapInfo";
	}

	public static string ItemUpgrade(string itemID)
	{
		ItemType itemType = Game.DataManager.GetItemData(itemID).ItemType;
		return $"{itemType}_{itemID}_Upgrade";
	}

	public static string PartCategory(PartType partType)
	{
		return $"Part_{partType}";
	}

	public static string ServiceName(ServiceName serviceName)
	{
		return $"UI_Service_{serviceName}_Name";
	}

	public static string ServiceTypeName(FieldServiceType serviceType)
	{
		return $"UI_ServiceType_{serviceType}_Name";
	}

	public static string UpgradeWarning(UpgradeCheckResult result)
	{
		return $"UI_Upgrade_Warning_{result}";
	}

	public static string ServiceWarning(FieldServiceType serviceType, UpgradeCheckResult result)
	{
		return $"UI_{serviceType}_Warning_{result}";
	}

	public static string SproutWarning(RegionName regionName)
	{
		return $"UI_{FieldServiceType.SproutReward}_Warning_{regionName}";
	}

	public static string CoreStoneWarning(TraitResult result)
	{
		return $"UI_CoreStone_Warning_{result}";
	}

	public static string CoreStoneWarningCannotSwitch()
	{
		return "UI_CoreStone_Warning_CannotSwitchSkillAtAltar";
	}

	public static string PartWarning(string warningType)
	{
		return "UI_Part_Warning_" + warningType;
	}

	public static string ConfirmPopupTitle(string type)
	{
		return "UI_ConfirmPopup_" + type + "_Title";
	}

	public static string ConfirmPopupDesc(string type)
	{
		return "UI_ConfirmPopup_" + type + "_Desc";
	}

	public static string StatName(StatType statType)
	{
		return "Stat_" + statType;
	}

	public static bool TryGetSkillKey(string skillName, out string localeKey)
	{
		localeKey = SkillKey(skillName);
		return Game.DataManager.IsLocaleExist(localeKey);
	}

	public static string SkillKey(string skillName)
	{
		return "CoreStone_" + skillName + "_Key";
	}

	public static string CoreStonePassiveDesc(string coreStoneName, int rank)
	{
		return $"CoreStone_{coreStoneName}_Passive_Rank{rank}_Desc";
	}

	public static string CoreStoneEngravingDesc(string coreStoneName, int rank)
	{
		return $"CoreStone_{coreStoneName}_Engraving_Rank{rank}_Desc";
	}

	public static string CoreStoneEngravingTitle(int rank)
	{
		return $"UI_Upgrade_Engraving_Title_Rank{rank}";
	}

	public static string MapLegendName(MapObjectType type)
	{
		return "UI_Map_Legend_" + type;
	}

	public static string TutorialName(ActionName actionName)
	{
		return $"Tutorial_{actionName}_SkillName";
	}

	public static string TutorialActionDesc(ActionName actionName)
	{
		string arg = "";
		if (actionName == ActionName.Parry && Game.Instance.SaveData.Difficulty == Difficulty.Story)
		{
			arg = "_Difficulty_Story";
		}
		return $"Tutorial_{actionName}_ActionDesc{arg}";
	}

	public static string BossClearResonance(BossName boss)
	{
		return $"{boss}Clear_Resonance";
	}

	public static string DifficultyName(Difficulty difficulty)
	{
		return $"Title_Difficulty_{difficulty}_Name";
	}

	public static string DifficultyDesc(Difficulty difficulty)
	{
		return $"Title_Difficulty_{difficulty}_Desc";
	}

	public static string AchievementName(string achievementId)
	{
		return "Achievement_" + achievementId + "_Name";
	}
}
