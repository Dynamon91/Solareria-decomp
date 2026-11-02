using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SPR.UI.Store;
using SPR.UI.Upgrade;

namespace SPR;

public abstract class StoreNPC : ServiceNPC<StoreType>, IStore
{
	public virtual bool CanPurchaseMultiple => false;

	public int PreviousSalesCount { get; protected set; }

	protected override ServicePanel<StoreType> ServicePanel => Npc.FieldCanvas.StorePanel;

	public override async UniTask OpenServiceAsync()
	{
		if ((object)serviceVcam == null)
		{
			DebugLog.GeneralLogError($"{npcName}에게 Store용 Vcam이 할당되지 않았습니다!");
		}
		ServicePanel.Initialize(this, serviceVcam, panelArg);
		PreviousSalesCount = 0;
		if (ServicePanel.HasStock())
		{
			await Npc.InputManager.StackControlTarget(ServicePanel);
		}
		else
		{
			PlaySoldOutScenario();
		}
	}

	public abstract override void OnServiceOpening();

	public abstract override void OnServiceClosing();

	public async UniTask PurchaseItemAsync(StoreItemData storeItemData)
	{
		await PurchaseItemsAsync(new List<StoreItemData> { storeItemData });
	}

	public async UniTask PurchaseItemsAsync(List<StoreItemData> storeItemDatas)
	{
		PreviousSalesCount += storeItemDatas.Count;
		await OnPurchaseSucceededAsync(storeItemDatas);
	}

	protected virtual UniTask OnPurchaseSucceededAsync(List<StoreItemData> storeItemDatas)
	{
		return default(UniTask);
	}
}
