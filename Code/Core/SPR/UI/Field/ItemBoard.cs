using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR.UI.Field;

public class ItemBoard : MonoBehaviour
{
	private ItemBoardComponent boardComponent;

	private readonly Queue<ItemBoardData> dataQueue = new Queue<ItemBoardData>();

	private bool isWaiting;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static PlayerController Player => Game.Instance.Player;

	private static FieldController FieldController => Game.Instance.FieldController;

	private static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	public void Initialize()
	{
		boardComponent = GetComponentInChildren<ItemBoardComponent>(includeInactive: true);
		boardComponent.Initialize(delegate
		{
			StartBusyWaiting().Forget();
		});
		dataQueue.Clear();
		base.gameObject.SetActive(value: true);
	}

	private void Update()
	{
		if (!boardComponent.CanAnimatingOnlyPlayer || InputManager.IsCurrentControlTarget(Player))
		{
			boardComponent.OnUpdate();
		}
	}

	public void ShowDataImmediately(string itemID)
	{
		ShowDataImmediatelyAsync(itemID).Forget();
	}

	public void EnqueueItemData(string itemID)
	{
		EnqueueItemDataAsync(itemID).Forget();
	}

	private async UniTask ShowDataImmediatelyAsync(string itemID)
	{
		boardComponent.SkipAsync().Forget();
		ItemBoardData itemBoardData = await ItemBoardData.GetDataAsync(itemID);
		await WaitForCorrectTimeAsync();
		boardComponent.Activate(itemBoardData);
	}

	private async UniTask EnqueueItemDataAsync(string itemID)
	{
		ItemBoardData item = await ItemBoardData.GetDataAsync(itemID);
		dataQueue.Enqueue(item);
		if (!isWaiting)
		{
			StartBusyWaiting().Forget();
		}
	}

	private async UniTask StartBusyWaiting()
	{
		isWaiting = true;
		await WaitForCorrectTimeAsync();
		await UniTask.WaitUntil(() => InputManager.IsCurrentControlTarget(Player));
		if (dataQueue.Count != 0)
		{
			ItemBoardData data = dataQueue.Dequeue();
			boardComponent.Activate(data);
		}
		isWaiting = false;
	}

	private async UniTask WaitForCorrectTimeAsync()
	{
		await UniTask.WaitWhile(() => boardComponent.IsAnimating);
		await UniTask.WaitWhile(() => !InputManager.IsCurrentControlTarget(Player) || FieldController.GetCutsceneProducers().Any((CutsceneProducer producer) => producer.IsProducing));
		await UniTask.WaitWhile(() => FieldCanvas.IsPending);
	}

	public void ShowLevelUpInflareImmediately(int count)
	{
		ShowInflareImmediatelyAsync(count).Forget();
	}

	public void Skip()
	{
		boardComponent.SkipAsync().Forget();
	}

	public void ActivateInflareEffect()
	{
		boardComponent.ActivateInflareEffect();
	}

	private async UniTask ShowInflareImmediatelyAsync(int count)
	{
		ItemBoardData itemBoardData = await ItemBoardData.GetDataAsync("Inflare", count);
		await UniTask.WaitWhile(() => boardComponent.IsAnimating);
		await UniTask.WaitWhile(() => FieldCanvas.IsPending);
		boardComponent.Activate(itemBoardData);
	}
}
