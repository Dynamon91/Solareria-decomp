using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SPR.UI.Upgrade;

public class UpgradeWarmhornCountPanel : UpgradePanel
{
	[Header("Key Guides")]
	[SerializeField]
	private List<GameObject> upgradeControlGuides;

	protected override bool UsingOhn => true;

	private UpgradeWarmhornCountBoard WarmhornCountBoard => upgradeBoard as UpgradeWarmhornCountBoard;

	protected override string ConfirmType => "WarmhornCount";

	public override string ControllerName()
	{
		return "Upgrade Warmhorn Count Panel";
	}

	protected override async UniTask OnActivateAsync()
	{
		await upgradeBoard.InitializeAsync();
		WarmhornCountBoard.SetData(new UpgradeWarmhornCountModel(ServicePanel<Unit>.PlayerStat.GetStat<WarmhornCount>().MaxPotionCount));
		UpdateControlGuide();
	}

	protected override void OnControlInput(int horizontal, int vertical)
	{
	}

	protected override void OnConfirmInput()
	{
		upgradeBoard.OnProceedUpgrade();
		ServicePanel<Unit>.FieldCanvas.InteractionPanel.ShowDataImmediately(ItemType.Warmhorn, "Warmhorn");
		ServicePanel<Unit>.FieldCanvas.MoneyBoard.ActivateContinuous(value: false);
		UpdateData();
		ServicePanel<Unit>.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Shop_Buy");
	}

	public override UniTask Pause()
	{
		return UniTask.CompletedTask;
	}

	public override UniTask Restart()
	{
		ServicePanel<Unit>.FieldCanvas.MoneyBoard.ActivateContinuous(value: true);
		return UniTask.CompletedTask;
	}

	protected override void UpdateControlGuide()
	{
		if (upgradeBoard.CanProceedUpgrade() == UpgradeCheckResult.Success)
		{
			upgradeControlGuides.ForEach(delegate(GameObject g)
			{
				g.SetActive(value: true);
			});
		}
		else
		{
			upgradeControlGuides.ForEach(delegate(GameObject g)
			{
				g.SetActive(value: false);
			});
		}
	}

	public override void UpdateData()
	{
		WarmhornCountBoard.SetData(new UpgradeWarmhornCountModel(ServicePanel<Unit>.PlayerStat.GetStat<WarmhornCount>().MaxPotionCount));
		UpdateControlGuide();
	}
}
