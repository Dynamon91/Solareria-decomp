using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace SPR.Field;

public class CookMachine : ServiceObject, IStore
{
	public bool CanPurchaseMultiple => false;

	public UniTask PurchaseItemAsync(StoreItemData storeItemData)
	{
		return default(UniTask);
	}

	public UniTask PurchaseItemsAsync(List<StoreItemData> storeItemDatas)
	{
		return default(UniTask);
	}
}
