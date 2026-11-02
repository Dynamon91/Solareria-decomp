using Library;
using SPR.Achievement;
using SPR.Save;
using SPR.UI.Field;
using SPR.UI.Map;

namespace SPR;

public class PureItem
{
	public string ItemName { get; }

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	private static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	public PureItem(string itemName)
	{
		ItemName = itemName;
	}

	public void AddToPlayerData(ItemAcquireDisplayOrder order = ItemAcquireDisplayOrder.None, bool ignoreIfAlreadyOwned = false)
	{
		ItemData itemData = Game.DataManager.GetItemData(ItemName);
		if (itemData == null)
		{
			DebugLog.DataManagerLogError(ItemName + "라는 아이템을 DataManager에서 찾을 수 없습니다.");
		}
		else
		{
			if (ignoreIfAlreadyOwned && CheckIfAlreadyOwned(itemData.ItemType, ItemName))
			{
				return;
			}
			if (order == ItemAcquireDisplayOrder.ShowFullPanelImmediately)
			{
				FieldCanvas.InteractionPanel.ShowDataImmediately(itemData.ItemType, ItemName);
			}
			else
			{
				ItemAcquireDisplayType itemAcquireDisplayType = GetItemAcquireDisplayType(ItemName, order);
				DisplayItemAcquire(itemAcquireDisplayType);
			}
			switch (itemData.ItemType)
			{
			case ItemType.CoreStone:
				PlayerData.AddCardData(ItemName);
				break;
			case ItemType.Part:
				Game.Instance.Player.PlayerPart.AddPart(ItemName);
				break;
			case ItemType.Inflare:
				PlayerData.AddTotalTraitPoint(1);
				break;
			case ItemType.InfoStone:
				PlayerData.AddAcquiredItem(ItemName);
				MenuMapPanel.AddInfoStoneInformation(ItemName);
				break;
			case ItemType.MapMarker:
				PlayerData.AddAcquiredItem(ItemName);
				SpecialActionForMapMarker(ItemName);
				break;
			case ItemType.PlagueSprout:
				PlayerData.AddAcquiredItem(ItemName);
				PlayerData.AddShadowSproutCount(Game.FieldID.GetRegionName(), 1);
				break;
			case ItemType.Note:
			case ItemType.Collectable:
			{
				PlayerData.AddAcquiredItem(ItemName);
				MapNoticeData mapNoticeData = DataManager.Instance.GetMapNoticeData(ItemName);
				if (mapNoticeData != null)
				{
					MenuMapPanel.AddInfoStoneInformation(mapNoticeData.TargetID);
				}
				break;
			}
			default:
				PlayerData.AddAcquiredItem(ItemName);
				break;
			}
			switch (itemData.ID)
			{
			case "ResidualHeat":
				FieldCanvas.InteractionPanel.OnDeactivate += delegate
				{
					LazySingleton<MessageSystem>.Instance.Publish(new HeatHazeTutorialEvent());
				};
				break;
			case "Counter":
				FieldCanvas.InteractionPanel.OnDeactivate += delegate
				{
					LazySingleton<MessageSystem>.Instance.Publish(new CounterTutorialEvent());
				};
				break;
			case "DashPassWall":
				FieldCanvas.InteractionPanel.OnDeactivate += delegate
				{
					LazySingleton<MessageSystem>.Instance.Publish(new DashPassWallTutorialEvent());
				};
				break;
			case "JumpDashAttack":
				FieldCanvas.InteractionPanel.OnDeactivate += delegate
				{
					LazySingleton<MessageSystem>.Instance.Publish(new JumpDashAttackTutorialEvent());
				};
				break;
			case "Heat":
				FieldCanvas.InteractionPanel.OnDeactivate += delegate
				{
					LazySingleton<MessageSystem>.Instance.Publish(new HeatCoreStoneTutorialEvent());
				};
				break;
			case "CardamineLetter":
				FieldCanvas.InteractionPanel.OnDeactivate += delegate
				{
					Game.Instance.UIManager.MenuCanvas.MemoryPanel.UpdateMemory(ActorName.Cardamine, isClear: true);
				};
				break;
			}
			AchievementHelper.CheckTriggers();
		}
	}

	public static ItemAcquireDisplayType GetItemAcquireDisplayType(string itemName, ItemAcquireDisplayOrder order)
	{
		switch (order)
		{
		case ItemAcquireDisplayOrder.ShowFullPanel:
		case ItemAcquireDisplayOrder.ShowFullPanelImmediately:
			return ItemAcquireDisplayType.FullPanel;
		case ItemAcquireDisplayOrder.ShowNotification:
			return ItemAcquireDisplayType.Notification;
		default:
		{
			ItemData itemData = Game.DataManager.GetItemData(itemName);
			switch (itemData.ItemType)
			{
			case ItemType.Inflare:
			case ItemType.Food:
			case ItemType.MapMarker:
			case ItemType.Collectable:
			case ItemType.CookingLog:
			case ItemType.GuardianStone:
			case ItemType.LongNote:
				return ItemAcquireDisplayType.Notification;
			case ItemType.Ingredient:
			case ItemType.Gem:
			case ItemType.InfoStone:
			case ItemType.PlagueSprout:
				if (!PlayerData.HasEverObtainedItemType(itemData.ItemType))
				{
					return ItemAcquireDisplayType.FullPanel;
				}
				return ItemAcquireDisplayType.Notification;
			case ItemType.Material:
			{
				if (!PlayerData.HasEverObtainedItem(itemName, out var _))
				{
					return ItemAcquireDisplayType.FullPanel;
				}
				return ItemAcquireDisplayType.Notification;
			}
			default:
				return ItemAcquireDisplayType.FullPanel;
			}
		}
		}
	}

	private void DisplayItemAcquire(ItemAcquireDisplayType displayType)
	{
		switch (displayType)
		{
		case ItemAcquireDisplayType.FullPanel:
			FieldCanvas.InteractionPanel.EnqueueItemData(ItemName);
			break;
		case ItemAcquireDisplayType.Notification:
			FieldCanvas.ItemBoard.EnqueueItemData(ItemName);
			break;
		}
	}

	private bool CheckIfAlreadyOwned(ItemType type, string itemName)
	{
		int current;
		return type switch
		{
			ItemType.CoreStone => PlayerData.HasCoreStone(itemName), 
			ItemType.Part => PlayerData.GetPartLevel(itemName) >= 0, 
			ItemType.Inflare => false, 
			_ => PlayerData.HasEverObtainedItem(itemName, out current), 
		};
	}

	private void SpecialActionForMapMarker(string itemName)
	{
		MapData mapData = Game.Instance.SaveData.WorldData.MapData;
		int markerType = MapMarkerIcon.GetMarkerType(itemName);
		if (markerType < 0)
		{
			return;
		}
		if (markerType > 0)
		{
			if (mapData.GetSpecialStatus(markerType) == MapMarkerStatus.Unavailable)
			{
				mapData.SetSpecialStatus(markerType, MapMarkerStatus.Standby);
			}
			else
			{
				DebugLog.GeneralLogWarning("무하에게서 구입한 지도 특수 마커(" + itemName + ")는 이미 획득한 상태입니다!");
			}
		}
		else
		{
			DebugLog.GeneralLogError("무하에게서 구입한 아이템(" + itemName + ")이 유효한 지도 마커가 아닙니다!");
		}
	}
}
