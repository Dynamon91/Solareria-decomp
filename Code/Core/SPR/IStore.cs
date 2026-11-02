using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace SPR;

public interface IStore
{
	bool CanPurchaseMultiple { get; }

	UniTask PurchaseItemAsync(StoreItemData storeItemData);

	UniTask PurchaseItemsAsync(List<StoreItemData> storeItemDatas);
}
