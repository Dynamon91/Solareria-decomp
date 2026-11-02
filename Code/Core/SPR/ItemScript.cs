using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SPR.UI.Talk;

namespace SPR;

public class ItemScript : Script
{
	private enum Order
	{
		Give,
		Remove
	}

	private readonly string itemName;

	private readonly Order order;

	public override bool IsInstant => true;

	public ItemScript(IReadOnlyList<string> values)
	{
		itemName = values[0];
		order = Enum.Parse<Order>(values[1]);
	}

	public override void Run(TalkCanvas talkCanvas)
	{
		switch (order)
		{
		case Order.Give:
			GiveItemAsync(talkCanvas).Forget();
			break;
		case Order.Remove:
			if (!Game.Instance.SaveData.PlayerData.TryRemoveAcquiredItem(itemName))
			{
				DebugLog.ScenarioLogError(itemName + " 아이템을 가지고 있지 않아 제거할 수 없습니다.");
			}
			talkCanvas.RunNextScript();
			break;
		}
	}

	public override void Skip(TalkCanvas talkCanvas)
	{
		switch (order)
		{
		case Order.Give:
			new PureItem(itemName).AddToPlayerData();
			break;
		case Order.Remove:
			if (!Game.Instance.SaveData.PlayerData.TryRemoveAcquiredItem(itemName))
			{
				DebugLog.ScenarioLogError(itemName + " 아이템을 가지고 있지 않아 제거할 수 없습니다.");
			}
			break;
		}
		talkCanvas.RunNextScript_Skip();
	}

	private async UniTask GiveItemAsync(TalkCanvas talkCanvas)
	{
		await talkCanvas.DeactivateTalkBoardAsync();
		talkCanvas.IsTalking = false;
		bool watched = false;
		Script.FieldCanvas.InteractionPanel.OnDeactivate += delegate
		{
			watched = true;
		};
		new PureItem(itemName).AddToPlayerData(ItemAcquireDisplayOrder.ShowFullPanelImmediately);
		await UniTask.WaitUntil(() => watched);
		talkCanvas.RunNextScript();
	}
}
