using Cysharp.Threading.Tasks;
using SPR.Save;
using SPR.UI.Upgrade;

namespace SPR;

public class Flutis : StoreNPC
{
	private static PartUpgradePanel UpgradePanel => Game.Instance.UIManager.FieldCanvas.PartUpgradePanel;

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		SetAnimation(base.SkeletonAnimation.AnimationName);
	}

	public async UniTask OpenHoningPanelAsync()
	{
		UpgradePanel.Initialize(this, serviceVcam, default(Unit));
		if (UpgradePanel.HasStock())
		{
			await Npc.InputManager.StackControlTarget(UpgradePanel);
		}
		else
		{
			PlaySoldOutScenario();
		}
	}

	public override void OnServiceOpening()
	{
		serviceVcam.SetActive(value: true);
	}

	public override void OnServiceClosing()
	{
		serviceVcam.SetActive(value: false);
	}

	public void OnHoningSucceeded()
	{
		base.PreviousSalesCount++;
	}

	protected override void EnterIdleAnimation()
	{
		SetAnimation("idle_b");
	}
}
