using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SPR.Save;

namespace SPR.Field;

public class MuhaHouseJar : ServiceObject, IStore
{
	public virtual bool CanPurchaseMultiple => false;

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
	}

	public async UniTask PurchaseItemAsync(StoreItemData storeItemData)
	{
		await PurchaseItemsAsync(new List<StoreItemData> { storeItemData });
	}

	public async UniTask PurchaseItemsAsync(List<StoreItemData> storeItemDatas)
	{
		await OnPurchaseSucceededAsync(storeItemDatas);
	}

	protected virtual UniTask OnPurchaseSucceededAsync(List<StoreItemData> storeItemDatas)
	{
		return default(UniTask);
	}
}
