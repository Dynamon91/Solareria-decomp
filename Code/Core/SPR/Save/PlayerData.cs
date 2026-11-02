using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Library;
using SPR.Achievement;
using SPR.UI.Map;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class PlayerData
{
	[Serializable]
	public class SkillData
	{
		[SerializeField]
		public string typeName;

		[SerializeField]
		public bool active;
	}

	[Serializable]
	public class DropData
	{
		[SerializeField]
		private FieldID fieldID;

		[SerializeField]
		private Vector2 worldPosition;

		[SerializeField]
		private Vector2 areaNormalizedPosition;

		[SerializeField]
		private int experience;

		[SerializeField]
		private int money;

		public FieldID FieldID => fieldID;

		public Vector2 WorldPos => worldPosition;

		public Vector2 NormalizedPos => areaNormalizedPosition;

		public int Exp => experience;

		public int Money => money;

		public bool IsValid => fieldID.SectorName != SectorName.None;

		public DropData()
		{
			Clear();
		}

		public DropData(FieldID fieldID, Vector2 pos, Vector2 normalizedPos, int exp, int money)
		{
			this.fieldID = fieldID;
			worldPosition = pos;
			areaNormalizedPosition = normalizedPos;
			experience = exp;
			this.money = money;
		}

		public void Clear()
		{
			fieldID = new FieldID(SectorName.None, -1);
			worldPosition = Vector2.zero;
			areaNormalizedPosition = Vector2.zero;
			experience = 0;
			money = 0;
		}

		internal void ChangePos(Vector2 pos, Vector2 normalizedPos)
		{
			worldPosition = pos;
			areaNormalizedPosition = normalizedPos;
		}
	}

	[Serializable]
	public class ShadowSproutData
	{
		[Serializable]
		public class ShadowSproutInfo
		{
			[SerializeField]
			private RegionName regionName;

			[SerializeField]
			private int sproutCount;

			[SerializeField]
			private int rewardCount;

			public RegionName RegionName => regionName;

			public int SproutCount => sproutCount;

			public int RewardCount => rewardCount;

			public ShadowSproutInfo(RegionName regionName, int sproutCount, int rewardCount)
			{
				this.regionName = regionName;
				this.sproutCount = sproutCount;
				this.rewardCount = rewardCount;
			}

			public void AddCount(int value)
			{
				sproutCount += value;
			}

			public void AddRewardCount(int value)
			{
				rewardCount += value;
			}
		}

		[SerializeField]
		private List<ShadowSproutInfo> shadowSproutPairs;

		public ShadowSproutData()
		{
			shadowSproutPairs = new List<ShadowSproutInfo>(7);
			for (int i = 1; i < 7; i++)
			{
				shadowSproutPairs.Add(new ShadowSproutInfo((RegionName)i, 0, 0));
			}
		}

		public int GetCount(RegionName regionName)
		{
			return shadowSproutPairs.First((ShadowSproutInfo pair) => pair.RegionName == regionName).SproutCount;
		}

		public void AddCount(RegionName regionName, int value)
		{
			shadowSproutPairs.First((ShadowSproutInfo pair) => pair.RegionName == regionName).AddCount(value);
		}

		public void AddRewardCount(RegionName regionName, int value)
		{
			shadowSproutPairs.First((ShadowSproutInfo pair) => pair.RegionName == regionName).AddRewardCount(value);
		}

		public int GetRewardCount(RegionName regionName)
		{
			return shadowSproutPairs.First((ShadowSproutInfo pair) => pair.RegionName == regionName).RewardCount;
		}
	}

	[Serializable]
	public class PartData
	{
		[SerializeField]
		private PartType partType;

		[SerializeField]
		private string partID;

		[SerializeField]
		private int level;

		public PartType PartType => partType;

		public string PartID => partID;

		public int Level => level;

		public PartData(PartType partType, string id)
		{
			this.partType = partType;
			partID = id;
			level = 0;
		}

		public void SetLevel(int value)
		{
			level = value;
		}
	}

	[SerializeField]
	private int level;

	[SerializeField]
	private int currentExp;

	[SerializeField]
	private int money;

	[SerializeField]
	private int deathCount;

	[SerializeField]
	private int totalTraitPoint;

	[SerializeField]
	private int potionCount;

	[SerializeField]
	private List<string> recordedItemIDList = new List<string>();

	[SerializeField]
	private List<string> acquiredItemIDList = new List<string>();

	[SerializeField]
	private List<CardData> cardDataList = new List<CardData>();

	[SerializeField]
	private List<SkillData> skillDataList = new List<SkillData>();

	[SerializeField]
	private DropData dropData = new DropData();

	[SerializeField]
	private ShadowSproutData shadowSproutData = new ShadowSproutData();

	[SerializeField]
	private int heatLevel;

	[SerializeField]
	private List<string> equippedPartIDList = new List<string>();

	[SerializeField]
	private List<PartData> acquiredPartList = new List<PartData>();

	[SerializeField]
	private List<string> appliedFoodIDList = new List<string>();

	public const int InitialLevel = 1;

	public const int MaximumLevel = 120;

	public const int InitialInflare = 1;

	private const int MaxInflare = 160;

	public const string BasicWeaponName = "PyronWristband";

	public const int InitialPotionCount = 3;

	public const int InitialPotionAmount = 20;

	public const int PotionHealIncreasePerUpgrade = 5;

	public int Level => level;

	public int CurrentExp => currentExp;

	public int Money => money;

	public int DeathCount => deathCount;

	public int PotionInitialCount => potionCount;

	public int TotalTraitPoint => totalTraitPoint;

	public int AvailableTraitPoint => totalTraitPoint - cardDataList.Sum((CardData x) => x.IsStatCard ? x.RawLevel : 0);

	public int HeatLevel => heatLevel;

	public IReadOnlyList<string> AcquiredItemIDList => acquiredItemIDList;

	public IReadOnlyList<string> AppliedFoodIDList => appliedFoodIDList;

	public IReadOnlyList<(string itemID, int quantity)> AcquiredItems
	{
		get
		{
			List<(string, int)> list = new List<(string, int)>();
			foreach (string id in acquiredItemIDList)
			{
				if (!list.Any(((string, int) x) => x.Item1 == id))
				{
					int item = acquiredItemIDList.Count((string x) => x == id);
					list.Add((id, item));
				}
			}
			return list;
		}
	}

	public IReadOnlyList<string> RecordedItemIDList => recordedItemIDList;

	public IReadOnlyList<CardData> CardDataList => cardDataList;

	public IReadOnlyList<SkillData> SkillDataList => skillDataList;

	public IReadOnlyList<PartData> EquippedPartList => acquiredPartList.Where((PartData p) => equippedPartIDList.Contains(p.PartID)).ToList();

	public IReadOnlyList<PartData> AcquiredPartList => acquiredPartList;

	public DropData CurrentDropData => dropData;

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	public event Action OnTotalTraitPointChanged = delegate
	{
	};

	public event Action OnCardLevelChanged = delegate
	{
	};

	public event Action OnPartAcquiredOrEquipped = delegate
	{
	};

	public PlayerData()
	{
		level = 1;
		currentExp = 0;
		money = 0;
		deathCount = 0;
		totalTraitPoint = 1;
		potionCount = 3;
		heatLevel = 0;
		skillDataList.Clear();
		foreach (Type item2 in from t in Assembly.GetAssembly(typeof(InputItem)).GetTypes()
			where t.IsSubclassOf(typeof(InputItem))
			select t)
		{
			SkillData item = new SkillData
			{
				typeName = item2.Name
			};
			skillDataList.Add(item);
		}
		List<Type> source = new List<Type>
		{
			typeof(HealInputItem),
			typeof(JumpUpInputItem),
			typeof(JumpDownInputItem),
			typeof(LookUpDownInputItem),
			typeof(RollingInputItem),
			typeof(TeleportInputItem)
		};
		foreach (SkillData data in skillDataList)
		{
			if (source.Any((Type x) => x.Name == data.typeName))
			{
				data.active = true;
			}
		}
		cardDataList = new List<CardData>();
		for (int num = 1; num <= 3; num++)
		{
			cardDataList.Add(new CardData(num));
			TraitData traitData = Game.DataManager.GetTraitData(num);
			acquiredItemIDList.Add(traitData.Name);
		}
		for (int num2 = -1; num2 >= -5; num2--)
		{
			for (int num3 = 0; num3 < 20; num3++)
			{
				string markerName = MapMarkerIcon.GetMarkerName(num2);
				acquiredItemIDList.Add(markerName);
				if (!HasEverObtainedItem(markerName, out var _))
				{
					recordedItemIDList.Add(markerName);
				}
			}
		}
	}

	public void SetLevel(int value)
	{
		level = value;
	}

	public void SetCurrentExp(int value)
	{
		currentExp = value;
	}

	public void SetMoney(int value)
	{
		money = value;
	}

	public void AddDeathCount(IAttackSource source, Vector2 deathPos, int amount = 1)
	{
		deathCount += amount;
	}

	public void SetPotionCount(int value)
	{
		potionCount = value;
	}

	public void AddTotalTraitPoint(int amount)
	{
		totalTraitPoint += amount;
		LazySingleton<MessageSystem>.Instance.Publish(new InflarePointTutorialEvent());
		this.OnTotalTraitPointChanged?.Invoke();
		if (totalTraitPoint == 160)
		{
			AchievementHelper.Unlock("GetAllInflares");
		}
	}

	public void SetHeatLevel(int value)
	{
		heatLevel = value;
	}

	public void ApplyAllFoodItems()
	{
		foreach (string appliedFoodID in appliedFoodIDList)
		{
			FoodData foodData = Game.DataManager.GetFoodData(appliedFoodID);
			if (foodData != null)
			{
				ApplyFoodData(foodData);
			}
		}
	}

	public void UseFoodItem(string id)
	{
		appliedFoodIDList.Add(id);
		if (appliedFoodIDList.Count == Game.DataManager.FoodDataList.Count)
		{
			AchievementHelper.Unlock("EatAllFoods");
		}
		FoodData foodData = Game.DataManager.GetFoodData(id);
		ApplyFoodData(foodData);
	}

	private void ApplyFoodData(FoodData data)
	{
		for (int i = 0; i < data.StatTypes.Count; i++)
		{
			StatType statType = data.StatTypes[i];
			float num = data.StatValues[i];
			PlayerStat playerStat = Game.Instance.Player.PlayerStat;
			if (statType == StatType.Exp)
			{
				playerStat.AddExperience((int)num);
			}
			else
			{
				playerStat.GetStat(statType).AddFoodStat(num);
			}
		}
	}

	public void AddAcquiredItem(string id)
	{
		bool flag = true;
		if (id.Equals("Inflare"))
		{
			AddTotalTraitPoint(1);
			return;
		}
		if (id == "Warmhorn")
		{
			PlayerStat playerStat = Game.Instance.Player.PlayerStat;
			playerStat.GetStat<Health>().AddHealth((int)playerStat.GetStat<WarmhornHealAmount>().Max, healInstantly: true);
			Game.Instance.SoundManager.PlayOneShot("event:/SFX/Player/System/SFX_Heal_Effect");
			return;
		}
		ItemType itemType = Game.DataManager.GetItemData(id).ItemType;
		if (itemType == ItemType.CoreStone || itemType == ItemType.MapMarker)
		{
			flag = false;
		}
		acquiredItemIDList.Add(id);
		if (!HasEverObtainedItem(id, out var _))
		{
			recordedItemIDList.Add(id);
		}
		if (flag)
		{
			WanderingData.AddUnwatchedInventoryItem(id);
		}
	}

	internal void AddAcquiredItem_Internal(string id, WanderingData wanderingData)
	{
		bool flag = true;
		if (id.Equals("Inflare"))
		{
			AddTotalTraitPoint(1);
			return;
		}
		if (id == "Warmhorn")
		{
			PlayerStat playerStat = Game.Instance.Player.PlayerStat;
			playerStat.GetStat<Health>().AddHealth((int)playerStat.GetStat<WarmhornHealAmount>().Max, healInstantly: true);
			Game.Instance.SoundManager.PlayOneShot("event:/SFX/Player/System/SFX_Heal_Effect");
			return;
		}
		ItemType itemType = Game.DataManager.GetItemData(id).ItemType;
		if (itemType == ItemType.CoreStone || itemType == ItemType.MapMarker)
		{
			flag = false;
		}
		acquiredItemIDList.Add(id);
		if (!HasEverObtainedItem(id, out var _))
		{
			recordedItemIDList.Add(id);
		}
		if (flag)
		{
			wanderingData.AddUnwatchedInventoryItem(id, invokeChanged: false);
		}
	}

	public bool TryRemoveAcquiredItem(string id, int num = 1, bool checkInventory = true)
	{
		if (GetAcquiredItemCount(id) < num)
		{
			return false;
		}
		for (int i = 0; i < num; i++)
		{
			acquiredItemIDList.Remove(id);
		}
		if (checkInventory && GetAcquiredItemCount(id) == 0)
		{
			WanderingData.RemoveUnwatchedInventoryItem(id);
		}
		return true;
	}

	public int GetAcquiredItemCount(string id)
	{
		return acquiredItemIDList.Count((string x) => x == id);
	}

	public bool HasEverObtainedItemType(ItemType itemType)
	{
		foreach (string recordedItemID in recordedItemIDList)
		{
			ItemData itemData = Game.DataManager.GetItemData(recordedItemID);
			if (itemData != null && itemData.ItemType == itemType)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasEverObtainedItem(string id, out int current)
	{
		bool flag = recordedItemIDList.Contains(id);
		current = (flag ? GetAcquiredItemCount(id) : 0);
		return flag;
	}

	public void AddAcquiredPart(PartType partType, string id)
	{
		if (!acquiredPartList.Any((PartData x) => x.PartID == id))
		{
			acquiredItemIDList.Add(id);
			acquiredPartList.Add(new PartData(partType, id));
			this.OnPartAcquiredOrEquipped?.Invoke();
		}
	}

	public void RemoveAcquiredPart(string id)
	{
		int num = acquiredPartList.FindIndex((PartData data) => data.PartID == id);
		if (num >= 0)
		{
			acquiredPartList.RemoveAt(num);
			this.OnPartAcquiredOrEquipped?.Invoke();
		}
	}

	public PartData GetEquippedPartData(string id)
	{
		return EquippedPartList.FirstOrDefault((PartData partData) => partData.PartID == id);
	}

	public void EquipPart(string id)
	{
		equippedPartIDList.Add(id);
		this.OnPartAcquiredOrEquipped?.Invoke();
	}

	public void UnequipPart(string id)
	{
		string text = equippedPartIDList.FirstOrDefault((string equippedID) => equippedID == id);
		if (text != null)
		{
			equippedPartIDList.Remove(text);
			this.OnPartAcquiredOrEquipped?.Invoke();
		}
	}

	public void SetPartLevel(string id, int level)
	{
		acquiredPartList.FirstOrDefault((PartData x) => x.PartID == id)?.SetLevel(level);
	}

	public int GetPartLevel(string id)
	{
		return acquiredPartList.FirstOrDefault((PartData x) => x.PartID == id)?.Level ?? (-1);
	}

	public void AddCardData(string coreStoneName)
	{
		TraitData traitData = Game.DataManager.GetTraitData(coreStoneName);
		if (traitData == null)
		{
			throw new ArgumentException("TraitData not found: " + coreStoneName);
		}
		AddCardData(traitData.ID);
	}

	public void AddCardData(int id)
	{
		CardData cardData = new CardData(id);
		cardDataList.Add(cardData);
		cardData.ApplyTraitEffect();
		WanderingData.AddUnwatchedCoreStone(id);
		TraitData traitData = Game.DataManager.GetTraitData(id);
		acquiredItemIDList.Add(traitData.Name);
	}

	internal void AddCardData_Internal(int id, WanderingData wanderingData)
	{
		CardData item = new CardData(id);
		cardDataList.Add(item);
		wanderingData.AddUnwatchedCoreStone(id, invokeChanged: false);
		TraitData traitData = Game.DataManager.GetTraitData(id);
		acquiredItemIDList.Add(traitData.Name);
	}

	internal bool RemoveCardData(int id, WanderingData wanderingData)
	{
		if (!TryGetCardData(id, out var cardData))
		{
			return false;
		}
		cardDataList.Remove(cardData);
		wanderingData.RemoveUnwatchedCoreStone(id, invokeChanged: false);
		TraitData traitData = Game.DataManager.GetTraitData(id);
		acquiredItemIDList.Remove(traitData.Name);
		return true;
	}

	public int GetCardLevel(int id)
	{
		foreach (CardData cardData in cardDataList)
		{
			if (cardData.ID == id)
			{
				return cardData.RawLevel;
			}
		}
		return -1;
	}

	public int GetCardRank(int id)
	{
		foreach (CardData cardData in cardDataList)
		{
			if (cardData.ID == id)
			{
				return cardData.Rank;
			}
		}
		return -1;
	}

	public bool TryGetCardData(int id, out CardData cardData)
	{
		foreach (CardData cardData2 in cardDataList)
		{
			if (cardData2.ID == id)
			{
				cardData = cardData2;
				return true;
			}
		}
		cardData = null;
		return false;
	}

	public void SetCardLevel(int id, int value)
	{
		foreach (CardData cardData in cardDataList)
		{
			if (cardData.ID == id)
			{
				cardData.SetLevel(value);
				this.OnCardLevelChanged?.Invoke();
				return;
			}
		}
		throw new IndexOutOfRangeException($"ID가 {id}인 카드를 보유하고 있지 않습니다.");
	}

	public void SetCardRank(int id, int rank)
	{
		foreach (CardData cardData in cardDataList)
		{
			if (cardData.ID == id)
			{
				cardData.SetRank(rank);
				return;
			}
		}
		throw new IndexOutOfRangeException($"ID가 {id}인 카드를 보유하고 있지 않습니다.");
	}

	public bool HasSkill()
	{
		return cardDataList.Any((CardData data) => !data.IsStatCard);
	}

	public bool HasCoreStone(string coreStoneName)
	{
		TraitData traitData = Game.DataManager.GetTraitData(coreStoneName);
		return cardDataList.Any((CardData data) => data.ID == traitData.ID);
	}

	public void SetSkillActive(InputItem item, bool active)
	{
		SkillData skillData = skillDataList.FirstOrDefault((SkillData data) => data.typeName == item.GetType().Name);
		if (skillData != null)
		{
			skillData.active = active;
		}
	}

	public void SetDropData(FieldID fieldID, Vector2 pos, Vector2 normalizedPos, int exp, int money)
	{
		dropData = new DropData(fieldID, pos, normalizedPos, exp, money);
	}

	public void ClearDropData()
	{
		dropData.Clear();
	}

	public int GetShadowSproutCount(RegionName regionName)
	{
		return shadowSproutData.GetCount(regionName);
	}

	public void AddShadowSproutCount(RegionName regionName, int value)
	{
		shadowSproutData.AddCount(regionName, value);
	}

	public void AddShadowRewardCount(RegionName regionName, int value)
	{
		shadowSproutData.AddRewardCount(regionName, value);
	}

	public int GetShadowRewardCount(RegionName regionName)
	{
		return shadowSproutData.GetRewardCount(regionName);
	}

	public List<StoreItemData> GetExchangeableItemData()
	{
		List<StoreItemData> list = new List<StoreItemData>();
		foreach (var acquiredItem in AcquiredItems)
		{
			string item = acquiredItem.itemID;
			int item2 = acquiredItem.quantity;
			ItemData itemData = Game.DataManager.GetItemData(item);
			if (itemData != null && itemData.ItemType == ItemType.Gem)
			{
				int price = Game.DataManager.GetGemExchangerData(item).Price;
				StoreItemData item3 = new StoreItemData(item, price, item2);
				list.Add(item3);
			}
		}
		return list.OrderBy((StoreItemData storeItemData) => storeItemData.Price).ToList();
	}

	public List<StoreItemData> GetAcquiredPartItemData(params PartType[] partTypes)
	{
		if (partTypes.Length == 0)
		{
			return new List<StoreItemData>();
		}
		return (from part in AcquiredPartList.Where((PartData part) => partTypes.Contains(part.PartType)).ToList()
			select new StoreItemData(part.PartID, 0, 1)).ToList();
	}

	public List<StoreItemData> GetEquippedPartItemData()
	{
		List<StoreItemData> list = new List<StoreItemData>();
		for (int i = 0; i < Enum.GetValues(typeof(PartType)).Length; i++)
		{
			PartType partType = (PartType)i;
			PartData partData = EquippedPartList.FirstOrDefault((PartData part) => part.PartType == partType);
			if (partData != null)
			{
				list.Add(new StoreItemData(partData.PartID, 0, 1));
			}
		}
		return list;
	}
}
