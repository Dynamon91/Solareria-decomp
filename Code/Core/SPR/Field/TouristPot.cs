using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMODUnity;
using SPR.Save;
using Spine.Unity;
using UnityEngine;

namespace SPR.Field;

public class TouristPot : ServiceObject, IStore
{
	[Header("Tourist Pot")]
	[SerializeField]
	private BoneFollower flameBoneFollower;

	[SerializeField]
	private ParticleSystem loopFlameParticle;

	[SerializeField]
	private ParticleSystem oneShotFlameParticle;

	[SerializeField]
	private EventReference flameEvent;

	private bool isActive;

	public bool CanPurchaseMultiple => false;

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		isActive = false;
	}

	public async UniTask PurchaseItemAsync(StoreItemData storeItemData)
	{
		await UniTask.WaitForSeconds(0.25f);
		oneShotFlameParticle.gameObject.SetActive(value: true);
		oneShotFlameParticle.Play();
		ServiceObject.SoundManager.PlayOneShotAttached(flameEvent, base.transform);
		await UniTask.WaitForSeconds(1.5f);
		oneShotFlameParticle.gameObject.SetActive(value: false);
	}

	public UniTask PurchaseItemsAsync(List<StoreItemData> storeItemDatas)
	{
		return default(UniTask);
	}

	public override void OnServiceOpening()
	{
		ServiceObject.InputManager.BlockControlForTask(OpenServiceAsync(), "Ready For Cook").Forget();
	}

	public override void OnServiceClosing()
	{
		ServiceObject.PlayerMover.SetAnimation("idle");
	}

	private async UniTask OpenServiceAsync()
	{
		if (!isActive)
		{
			flameBoneFollower.SkeletonRenderer = ServiceObject.PlayerMover.SkeletonRenderer;
			base.VfxController.Play("Flame");
			ServiceObject.PlayerMover.SetAnimation("scene/traveler_cooker", loop: false, 1f, 0, 0f, vfxEnable: false);
			await UniTask.WaitUntil(() => ServiceObject.PlayerMover.IsAnimComplete(0));
			isActive = true;
		}
		ServiceObject.PlayerMover.SetAnimation("cam_up");
	}
}
