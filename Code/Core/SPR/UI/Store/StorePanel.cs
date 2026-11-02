using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Save;
using SPR.UI.Tutorial;
using SPR.UI.Upgrade;
using UnityEngine;

namespace SPR.UI.Store;

public class StorePanel : ServicePanel<StoreType>
{
	[SerializeField]
	private StoreItemInfoBoard itemInfoBoard;

	[SerializeField]
	private ItemListBoard itemListBoard;

	[SerializeField]
	private StoreFadeBoard storeFadeBoard;

	private List<StoreItemData> storeItemDatas = new List<StoreItemData>();

	private List<StoreItemData> currentPurchasedItemDatas = new List<StoreItemData>();

	private StoreData storeData;

	private Sequence fadeSeq;

	private CanvasGroup canvasGroup;

	public const float FadeDuration = 0.5f;

	protected override bool UsingOhn => true;

	private IStore StoreOwner => ServiceOwner as IStore;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	public StoreType CurrentStoreType { get; private set; }

	private bool SalesFood
	{
		get
		{
			StoreType currentStoreType = CurrentStoreType;
			return currentStoreType == StoreType.Restaurant || currentStoreType == StoreType.TouristPot;
		}
	}

	public override bool IsValid => !base.gameObject.IsNull();

	public override void Initialize(IService owner, GameObject vcam, StoreType storeType)
	{
		if (!(owner is IStore))
		{
			DebugLog.GeneralLogError("StorePanel에 IStore를 상속받지 않은 대상이 들어왔습니다!");
			return;
		}
		ServiceOwner = owner;
		ServiceVcam = vcam;
		CurrentStoreType = storeType;
		storeFadeBoard.Initialize("UI_Store_Message_Purchased");
		UpdateStoreItems();
	}

	public override bool HasStock()
	{
		return storeItemDatas.Count > 0;
	}

	private void UpdateStoreItems()
	{
		SectorName sectorName = Game.Instance.CurrentFieldID.SectorName;
		storeData = Game.Instance.SaveData.WorldData.StoreData;
		storeItemDatas.Clear();
		IReadOnlyList<StoreItemData> readOnlyList;
		switch (CurrentStoreType)
		{
		case StoreType.Restaurant:
			readOnlyList = DataManager.Instance.GetLeafaItemDataList();
			break;
		case StoreType.Peddler:
			readOnlyList = DataManager.Instance.GetJimaItemDataList();
			break;
		case StoreType.Vendor:
			readOnlyList = DataManager.Instance.GetPwoongItemDataList();
			break;
		case StoreType.Artifacts:
			readOnlyList = DataManager.Instance.GetCardamineItemDataList();
			break;
		case StoreType.TouristPot:
			readOnlyList = DataManager.Instance.GetTouristItemDataList();
			break;
		case StoreType.Painter:
			readOnlyList = DataManager.Instance.GetMuhaItemDataList();
			break;
		default:
			DebugLog.GeneralLogError($"상점 아이템을 제대로 불러올 수 없습니다! (type = {CurrentStoreType}, sector = {sectorName}) " + "StoreItemData가 제대로 작성 되었는지, 코드에서 제대로 호출하고 있는지 확인해주세요.");
			return;
		}
		foreach (StoreItemData item in readOnlyList)
		{
			if (DataManager.Instance.GetItemData(item.ID) == null)
			{
				DebugLog.GeneralLogError(item.ID + "에 해당하는 아이템을 찾을 수 없습니다! StoreItem 시트의 내용이 Item 시트와 일치하는지 확인해주세요.");
			}
			else if (Trigger.CheckTrigger(item.AvailableTrigger) && IsItemOnSale(item))
			{
				bool num = item.Quantity < 0;
				int num2 = storeData.GetSoldItemIDList(CurrentStoreType).Count((StoreItemData soldItem) => soldItem.ID == item.ID && soldItem.AvailableTrigger == item.AvailableTrigger);
				int quantity = item.Quantity;
				if (num)
				{
					storeItemDatas.Add(item);
				}
				else if (num2 < quantity)
				{
					StoreItemData item2 = new StoreItemData(item, quantity - num2);
					storeItemDatas.Add(item2);
				}
			}
		}
		StoreType currentStoreType = CurrentStoreType;
		if (currentStoreType != StoreType.Restaurant && currentStoreType != StoreType.TouristPot)
		{
			return;
		}
		List<StoreItemData> list = new List<StoreItemData>();
		List<StoreItemData> list2 = new List<StoreItemData>();
		foreach (StoreItemData storeItemData in storeItemDatas)
		{
			if (storeItemData.GetIngredientData().All((KeyValuePair<string, int> pair) => ServicePanel<StoreType>.PlayerData.GetAcquiredItemCount(pair.Key) >= pair.Value) && ServicePanel<StoreType>.PlayerStat.Money >= storeItemData.Price)
			{
				list.Add(storeItemData);
			}
			else
			{
				list2.Add(storeItemData);
			}
		}
		storeItemDatas = new List<StoreItemData>(list.Count + list2.Count);
		storeItemDatas.AddRange(list);
		storeItemDatas.AddRange(list2);
	}

	protected override async UniTask OnActivate()
	{
		await AddSpriteCaches();
		ServiceVcam.SetActive(value: true);
		Game.Instance.FieldController.CurrentArea.Vcam.gameObject.SetActive(value: false);
		currentPurchasedItemDatas.Clear();
		RefreshPanel(reset: true);
		CanvasGroup.alpha = 0f;
		base.gameObject.SetActive(value: true);
		ServiceOwner.OnServiceOpening();
		ServicePanel<StoreType>.FieldCanvas.ShowAllAsync(instant: false).Forget();
		await FadeAsync(isActive: true);
	}

	public override string ControllerName()
	{
		return "Store Panel";
	}

	public override void Control()
	{
		int verticalDownUI = ServicePanel<StoreType>.InputManager.verticalDownUI;
		if (verticalDownUI != 0)
		{
			bool moveUp = verticalDownUI > 0;
			if (!itemListBoard.CanMove(moveUp))
			{
				return;
			}
			itemListBoard.Move(moveUp);
			SetItemInfo();
			ServicePanel<StoreType>.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Move");
		}
		if (ServicePanel<StoreType>.InputManager.confirmDown)
		{
			StoreItemData selectedItemData = itemListBoard.SelectedItemData;
			if (selectedItemData == null)
			{
				return;
			}
			TryBuyItem(selectedItemData);
		}
		if (ServicePanel<StoreType>.InputManager.menuDown || ServicePanel<StoreType>.InputManager.cancelDown)
		{
			if (StoreOwner.CanPurchaseMultiple && currentPurchasedItemDatas.Count > 0)
			{
				RunPurchaseTask();
			}
			else
			{
				ServicePanel<StoreType>.InputManager.RemoveControlTarget(this).Forget();
			}
		}
		if (ServicePanel<StoreType>.InputManager.scroll != 0f)
		{
			itemInfoBoard.Scroll(ServicePanel<StoreType>.InputManager.scroll.GetSign());
		}
	}

	public override UniTask Pause()
	{
		return default(UniTask);
	}

	public override UniTask Restart()
	{
		return default(UniTask);
	}

	protected override async UniTask OnDeactivate()
	{
		if (!(ServiceOwner is Leafa) || HasStock())
		{
			Game.Instance.Player.mover.SetAnimation("idle");
		}
		ServiceVcam.SetActive(value: false);
		Game.Instance.FieldController.CurrentArea.Vcam.gameObject.SetActive(value: true);
		ServiceOwner.OnServiceClosing();
		FadeAsync(isActive: false).GetAwaiter().OnCompleted(delegate
		{
			base.gameObject.SetActive(value: false);
			ClearBoards();
		});
		currentPurchasedItemDatas.Clear();
		await UniTask.CompletedTask;
	}

	public async UniTask FadeInOutAsync(bool fadeIn)
	{
		if (fadeIn)
		{
			UniTask uniTask = FadeAsync(isActive: true);
			UniTask uniTask2 = ServicePanel<StoreType>.FieldCanvas.HideAllAsync(instant: false);
			RefreshPanel(reset: true);
			ServicePanel<StoreType>.FieldCanvas.MoneyBoard.ActivateContinuous(value: true);
			await UniTask.WhenAll(uniTask, uniTask2);
		}
		else
		{
			UniTask uniTask3 = FadeAsync(isActive: false);
			UniTask uniTask4 = ServicePanel<StoreType>.FieldCanvas.HideAllAsync(instant: false);
			ServicePanel<StoreType>.FieldCanvas.MoneyBoard.ActivateContinuous(value: false);
			await UniTask.WhenAll(uniTask3, uniTask4);
		}
	}

	private bool IsItemOnSale(StoreItemData storeItemData)
	{
		if (storeItemData.Quantity >= 0)
		{
			return storeItemData.Quantity > 0;
		}
		return true;
	}

	private void TryBuyItem(StoreItemData storeItemData)
	{
		if (!IsItemOnSale(storeItemData))
		{
			return;
		}
		if (ServicePanel<StoreType>.PlayerStat.Money < storeItemData.Price)
		{
			ShowCannotPurchase();
			return;
		}
		if (storeItemData.GetIngredientData().Any((KeyValuePair<string, int> kvp) => ServicePanel<StoreType>.PlayerData.GetAcquiredItemCount(kvp.Key) < kvp.Value))
		{
			ShowCannotPurchase();
			return;
		}
		ConfirmModel model = new ConfirmModel("BuyItem", delegate
		{
			BuyItem(storeItemData);
		});
		ServicePanel<StoreType>.ConfirmPopup.SetModel(model);
		ServicePanel<StoreType>.InputManager.StackControlTarget(ServicePanel<StoreType>.ConfirmPopup).Forget();
		void ShowCannotPurchase()
		{
			itemListBoard.SelectedItem.ShakeImage();
			ServicePanel<StoreType>.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Common_Negative");
		}
	}

	private void BuyItem(StoreItemData storeItemData)
	{
		ServicePanel<StoreType>.PlayerStat.AddMoney(-storeItemData.Price);
		foreach (KeyValuePair<string, int> item in from kvp in storeItemData.GetIngredientData()
			where !ServicePanel<StoreType>.PlayerData.TryRemoveAcquiredItem(kvp.Key, kvp.Value)
			select kvp)
		{
			DebugLog.GeneralLogError($"{item.Key} 재료가 {item.Value}개 필요한데 {ServicePanel<StoreType>.PlayerData.GetAcquiredItemCount(item.Key)}개 밖에 없습니다!");
		}
		storeData.AddSoldItem(CurrentStoreType, storeItemData);
		new PureItem(storeItemData.ID).AddToPlayerData();
		ServicePanel<StoreType>.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Shop_Buy");
		currentPurchasedItemDatas.Add(storeItemData);
		if (StoreOwner.CanPurchaseMultiple)
		{
			ServicePanel<StoreType>.InputManager.BlockControlForTask(ShowMessage(), "Purchase Message").Forget();
		}
		else
		{
			UniTask task = StoreOwner.PurchaseItemAsync(storeItemData);
			ServicePanel<StoreType>.InputManager.BlockControlForTask(task, "Purchase Item").Forget();
			task.GetAwaiter().OnCompleted(delegate
			{
				ServicePanel<StoreType>.InputManager.BlockControlForTask(ShowMessage(), "Purchase Message").Forget();
			});
		}
		TryShowFoodTutorial();
	}

	private async UniTask AddSpriteCaches()
	{
		List<UniTask> list = new List<UniTask>();
		foreach (StoreItemData storeItemData in storeItemDatas)
		{
			list.Add(AddressableHelper.PreloadSpriteAsync(Address.ItemSprite(storeItemData.ID)));
			foreach (string item in storeItemData.Ingredient)
			{
				list.Add(AddressableHelper.PreloadSpriteAsync(Address.ItemSprite(item)));
			}
		}
		await UniTask.WhenAll(list);
	}

	private void RefreshPanel(bool reset)
	{
		UpdateStoreItems();
		if (HasStock())
		{
			itemListBoard.SetItemList(storeItemDatas, reset);
			SetItemInfo();
			return;
		}
		if (StoreOwner.CanPurchaseMultiple)
		{
			RunPurchaseTask();
			return;
		}
		ServicePanel<StoreType>.InputManager.RemoveControlTarget(this).Forget();
		if (StoreOwner is StoreNPC storeNPC)
		{
			storeNPC.PlaySoldOutScenario(fromPanel: true);
		}
	}

	private void SetItemInfo()
	{
		if (StoreOwner is StoreNPC storeNPC)
		{
			itemInfoBoard.SetData(itemListBoard.SelectedItemData, storeNPC.OwnerName);
			return;
		}
		ActorName seller = Enum.Parse<ActorName>(ServiceOwner.Name);
		itemInfoBoard.SetData(itemListBoard.SelectedItemData, seller);
	}

	private async UniTask FadeAsync(bool isActive, bool isInstant = false)
	{
		CanvasGroup.DOKill();
		await CanvasGroup.DOAlpha(isActive ? 1 : 0, isInstant ? 0f : 0.5f).WaitAsync();
	}

	private async UniTask ShowMessage()
	{
		await storeFadeBoard.FadeInOutAsync(delegate
		{
			RefreshPanel(reset: false);
		});
	}

	private void RunPurchaseTask()
	{
		StoreOwner.PurchaseItemsAsync(currentPurchasedItemDatas).GetAwaiter().OnCompleted(delegate
		{
			ServicePanel<StoreType>.InputManager.RemoveControlTarget(this).Forget();
		});
	}

	private void ClearBoards()
	{
		itemInfoBoard.ClearBoard();
		itemListBoard.ClearItemBoards();
	}

	private void TryShowFoodTutorial()
	{
		if ((CurrentStoreType == StoreType.TouristPot || CurrentStoreType == StoreType.Restaurant) && Game.Instance.SaveData.WanderingData.ShouldShowTutorial(ActionName.Food))
		{
			base.OnDeactiveteEvent -= ShowFoodTutorial;
			base.OnDeactiveteEvent += ShowFoodTutorial;
		}
	}

	private void ShowFoodTutorial()
	{
		UniTask task = ShowFoodTutorialAsync();
		ServicePanel<StoreType>.InputManager.BlockControlForTask(task, "Food Tutorial").Forget();
	}

	private async UniTask ShowFoodTutorialAsync()
	{
		await UniTask.Delay(TimeSpan.FromSeconds(0.5));
		TutorialCanvas tutorialCanvas = Game.Instance.UIManager.TutorialCanvas;
		tutorialCanvas.SetData(ActionName.Food);
		ServicePanel<StoreType>.InputManager.StackControlTarget(tutorialCanvas).Forget();
	}
}
