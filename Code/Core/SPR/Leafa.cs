using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace SPR;

public class Leafa : StoreNPC
{
	private LeafaStoreCutsceneProducer cutsceneProducer;

	private bool isStoreOpen;

	public override bool CanPurchaseMultiple => true;

	private LeafaStoreCutsceneProducer CutsceneProducer => cutsceneProducer ?? (cutsceneProducer = GetComponentInChildren<LeafaStoreCutsceneProducer>());

	public override void Interact()
	{
		base.Interact();
		isStoreOpen = false;
		AddressableHelper.PreloadSpritesByLabelAsync("ItemSprite/Ingredient").Forget();
		AddressableHelper.PreloadSpritesByLabelAsync("ItemSprite/Food").Forget();
	}

	public override void OnServiceOpening()
	{
		isStoreOpen = true;
	}

	public override void OnServiceClosing()
	{
		isStoreOpen = false;
	}

	protected override async UniTask OnPurchaseSucceededAsync(List<StoreItemData> itemDatas)
	{
		Npc.FieldCanvas.StorePanel.FadeInOutAsync(fadeIn: false).Forget();
		PauseChasingSight();
		CutsceneProducer.PrepareInformations(itemDatas);
		await CutsceneProducer.PlayTimelineAsync(0, blockControl: true, hideCanvas: false);
		SetAnimation("idle");
		ResumeChasingSight();
		if (!Npc.FieldCanvas.StorePanel.HasStock())
		{
			PlaySoldOutScenario();
			Npc.MemoryPanel.TryUpdateClearedMemory(wasCleared: false, npcName);
		}
	}

	protected override void PlayTotAnimationOnPrepare(PlayerMover mover)
	{
		mover.SetAnimation("turn_back", loop: false);
		mover.AddAnimation("idle_back");
	}

	protected override void PlayTotAnimationOnFinished(PlayerMover mover)
	{
		if (!isStoreOpen)
		{
			base.PlayTotAnimationOnFinished(mover);
		}
	}

	protected override void OnFinishSoldOutTalk()
	{
		base.PlayTotAnimationOnFinished(Npc.Player.mover);
		base.OnFinishSoldOutTalk();
	}
}
