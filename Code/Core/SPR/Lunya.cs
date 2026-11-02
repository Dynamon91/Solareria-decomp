using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SPR.Save;
using SPR.UI.Upgrade;
using UnityEngine;

namespace SPR;

public class Lunya : StoreNPC
{
	[SerializeField]
	private List<GameObject> activeObjects;

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		SetAnimation(base.SkeletonAnimation.AnimationName);
		activeObjects.ForEach(delegate(GameObject go)
		{
			go.SetActive(base.gameObject.activeInHierarchy);
		});
	}

	protected override void EnterIdleAnimation()
	{
		SetAnimation("idle_b");
	}

	public void OpenCountUpgradePanel()
	{
		UpgradeWarmhornCountPanel upgradeWarmhornCountPanel = Npc.FieldCanvas.UpgradeWarmhornCountPanel;
		upgradeWarmhornCountPanel.Initialize(this, serviceVcam, default(Unit));
		Npc.InputManager.StackControlTarget(upgradeWarmhornCountPanel).Forget();
	}

	public override void OnServiceOpening()
	{
	}

	public override void OnServiceClosing()
	{
	}
}
