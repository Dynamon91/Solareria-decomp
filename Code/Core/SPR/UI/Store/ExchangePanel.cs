using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Library;
using SPR.UI.Upgrade;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SPR.UI.Store;

public class ExchangePanel : ServicePanel<Unit>
{
	[SerializeField]
	private ExchangeScrollBoard scrollBoard;

	[SerializeField]
	private ExchangeBoard exchangeBoard;

	[SerializeField]
	private StoreFadeBoard storeFadeBoard;

	[Header("Timeline Assets")]
	[SerializeField]
	private PlayableAsset openTimeline;

	[SerializeField]
	private PlayableAsset closeTimeline;

	private CanvasGroup canvasGroup;

	private PlayableDirector director;

	private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

	private PlayableDirector Director => director ?? (director = GetComponent<PlayableDirector>());

	protected override bool UsingOhn => true;

	public override bool IsValid => !BasicExtension.IsNull(base.gameObject);

	public override void Initialize(IService serviceObject, GameObject vcam, Unit arg)
	{
		ServiceOwner = serviceObject;
		ServiceVcam = vcam;
		storeFadeBoard.Initialize("UI_Store_Message_Exchanged");
	}

	public override bool HasStock()
	{
		return ServicePanel<Unit>.PlayerData.GetExchangeableItemData().Count > 0;
	}

	private async UniTask CachedSpritesAsync(List<string> idList)
	{
		foreach (string id in idList)
		{
			await CachedSpriteAsync(id);
		}
	}

	private async UniTask CachedSpriteAsync(string id)
	{
		if (id == "???")
		{
			id = "UnknownIngredient";
		}
		if (!spriteCache.ContainsKey(id))
		{
			AsyncOperationHandle<Sprite> task = Addressables.LoadAssetAsync<Sprite>(Address.ItemSprite(id));
			await task;
			spriteCache.Add(id, task.Result);
		}
	}

	private void MoveCursorToFirstCell()
	{
		scrollBoard.MoveCursorToFirstCell(delegate(ItemCell firstCell)
		{
			exchangeBoard.SetDataAndActivate(firstCell.Item, spriteCache);
		}, delegate
		{
			exchangeBoard.Deactivate();
		});
	}

	private void Exchange()
	{
		StoreItemData item = scrollBoard.CurrentCell.Item;
		int quantity = exchangeBoard.Quantity;
		for (int i = 0; i < quantity; i++)
		{
			if (!ServicePanel<Unit>.PlayerData.TryRemoveAcquiredItem(item.ID))
			{
				DebugLog.GeneralLogError($"{item.ID} 아이템을 {quantity}개 판매하려고 했는데 {i + 1}번째에 실패했습니다!");
				return;
			}
		}
		int totalPrice = exchangeBoard.TotalPrice;
		ServicePanel<Unit>.PlayerStat.AddMoney(totalPrice);
		ServicePanel<Unit>.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Shop_Buy");
		if (totalPrice > 0)
		{
			UniTask.WaitForSeconds(Game.Instance.UIManager.FieldCanvas.MoneyBoard.MaintainDuration, ignoreTimeScale: true).GetAwaiter().OnCompleted(delegate
			{
				ServicePanel<Unit>.SoundManager.PlayOneShot("event:/SFX/UI/Ingame/SFX_Coin_Long");
			});
		}
		UniTask task = storeFadeBoard.FadeInOutAsync(RefreshOrDeactivatePanel);
		ServicePanel<Unit>.InputManager.BlockControlForTask(task, "ExchangePanel Exchange Success").Forget();
	}

	private void RefreshOrDeactivatePanel()
	{
		List<StoreItemData> exchangeableItemData = ServicePanel<Unit>.PlayerData.GetExchangeableItemData();
		scrollBoard.SetData(exchangeableItemData, spriteCache);
		MoveCursorToFirstCell();
		if (!HasStock())
		{
			ExitAction();
		}
	}

	private void ExitAction()
	{
		Game.Instance.Player.mover.SetAnimation("idle");
		ServicePanel<Unit>.InputManager.RemoveControlTarget(this).Forget();
	}

	public override string ControllerName()
	{
		return "Exchange Panel";
	}

	protected override async UniTask OnActivate()
	{
		await OnActivateAsync();
		base.gameObject.SetActive(value: true);
		ServiceOwner.OnServiceOpening();
		ServiceVcam.SetActive(value: true);
		Director.playableAsset = openTimeline;
		Director.Play();
		await UniTask.WaitWhile(() => Director.state == PlayState.Playing);
	}

	private async UniTask OnActivateAsync()
	{
		List<StoreItemData> gems = ServicePanel<Unit>.PlayerData.GetExchangeableItemData();
		await CachedSpritesAsync(gems.Select((StoreItemData itemData) => itemData.ID).ToList());
		scrollBoard.SetData(gems, spriteCache);
		MoveCursorToFirstCell();
	}

	public override void Control()
	{
		int horizontalDownUI = ServicePanel<Unit>.InputManager.horizontalDownUI;
		int verticalDownUI = ServicePanel<Unit>.InputManager.verticalDownUI;
		bool confirmDown = ServicePanel<Unit>.InputManager.confirmDown;
		bool flag = ServicePanel<Unit>.InputManager.menuDown || ServicePanel<Unit>.InputManager.cancelDown;
		if (verticalDownUI != 0 && scrollBoard.TryMoveCursor(0, verticalDownUI, out var targetCell))
		{
			exchangeBoard.SetDataAndActivate(targetCell.Item, spriteCache);
			ServicePanel<Unit>.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
		}
		else if (horizontalDownUI != 0)
		{
			int value = exchangeBoard.Quantity + horizontalDownUI;
			exchangeBoard.SetQuantity(value, horizontalDownUI > 0);
		}
		else if (confirmDown && exchangeBoard.Quantity > 0)
		{
			Exchange();
		}
		else if (flag)
		{
			ExitAction();
		}
	}

	public override UniTask Pause()
	{
		return UniTask.CompletedTask;
	}

	public override UniTask Restart()
	{
		return UniTask.CompletedTask;
	}

	protected override async UniTask OnDeactivate()
	{
		Director.playableAsset = closeTimeline;
		Director.Play();
		await UniTask.WaitWhile(() => Director.state == PlayState.Playing);
		ServiceVcam.SetActive(value: false);
		base.gameObject.SetActive(value: false);
	}
}
