using System.Globalization;
using SPR.L10N;
using SPR.Library;
using SPR.Save;
using TMPro;
using UnityEngine;

namespace SPR.UI.Upgrade;

public abstract class UpgradeBoard<T> : UpgradeBoardBase where T : UpgradeModel
{
	[SerializeField]
	private TextMeshProUGUI upgradeCostText;

	protected T currentModel;

	protected static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	protected static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	private static FadeCanvas FadeCanvas => Game.Instance.UIManager.FadeCanvas;

	public void SetData(T model)
	{
		currentModel = model;
		UpdateUI();
	}

	protected void UpdateCostText(int upgradeCost, int currentMoney)
	{
		upgradeCostText.text = upgradeCost.ToString(CultureInfo.InvariantCulture);
		upgradeCostText.color = ((currentMoney >= upgradeCost) ? GraphicHelper.NormalColor : GraphicHelper.BadColor);
	}

	protected void UpdateFeedbackText(UpgradeCheckResult canProceedUpgrade, bool onProceed)
	{
		switch (canProceedUpgrade)
		{
		case UpgradeCheckResult.Success:
			ShowRequirements(value: true);
			HideWarning();
			break;
		case UpgradeCheckResult.NotEnoughOhn:
		case UpgradeCheckResult.NotEnoughMaterial:
			ShowRequirements(value: true);
			if (onProceed)
			{
				ShowWarning(canProceedUpgrade);
			}
			break;
		case UpgradeCheckResult.MaximumUpgrade:
			ShowRequirements(value: false);
			HideWarning();
			break;
		}
	}

	protected void ShowWarning(string localeKey)
	{
		FadeCanvas.WarningBoard.ShowWarning(localeKey);
	}

	private void ShowWarning(UpgradeCheckResult ucResult)
	{
		FadeCanvas.WarningBoard.ShowWarning(LocaleKey.UpgradeWarning(ucResult));
	}

	private void HideWarning()
	{
		FadeCanvas.WarningBoard.HideWarning();
	}
}
