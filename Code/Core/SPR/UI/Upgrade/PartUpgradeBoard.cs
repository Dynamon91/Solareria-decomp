using System.Linq;
using Coffee.UIExtensions;
using Cysharp.Threading.Tasks;
using Library;
using SPR.L10N;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Upgrade;

public class PartUpgradeBoard : UpgradeBoard<PartUpgradeModel>
{
	[SerializeField]
	private Image image;

	[SerializeField]
	private GameObject imageBackground;

	[SerializeField]
	private LocaleText partTypeText;

	[SerializeField]
	private LocaleText partNameText;

	[SerializeField]
	private LocaleText levelLocaleText;

	[SerializeField]
	private LocaleText descLocaleText;

	[SerializeField]
	private LocaleText upgradeInfoLocaleText;

	[SerializeField]
	private UIParticle equipParticle;

	[SerializeField]
	private GameObject costParent;

	public int Cost
	{
		get
		{
			string iD = currentModel.ID;
			int partLevel = Game.Instance.SaveData.PlayerData.GetPartLevel(iD);
			return Game.DataManager.GetPartData(currentModel.ID).UpgradeCosts[partLevel];
		}
	}

	public override UniTask InitializeAsync()
	{
		UpdateFeedbackText(UpgradeCheckResult.Success, onProceed: false);
		return UniTask.CompletedTask;
	}

	public bool HasStock()
	{
		return !Game.DataManager.PartDataList.All((PartData data) => UpgradeBoard<PartUpgradeModel>.PlayerData.GetPartLevel(data.ID) >= data.MaxLevel);
	}

	protected override void UpdateUI()
	{
		string iD = currentModel.ID;
		PartType partType = currentModel.PartType;
		image.sprite = currentModel.Sprite;
		if (iD.IsNullOrEmpty())
		{
			image.SetAlpha(4f / 51f);
			imageBackground.SetActive(value: false);
			partTypeText.ClearText();
			partNameText.ClearText();
			levelLocaleText.ClearText();
			descLocaleText.ClearText();
			upgradeInfoLocaleText.ClearText();
			ShowRequirements(value: false);
			return;
		}
		image.SetAlpha(1f);
		imageBackground.SetActive(value: true);
		partTypeText.SetData(LocaleKey.PartCategory(partType));
		partNameText.SetData(LocaleKey.ItemName(iD));
		int partLevel = Game.Instance.SaveData.PlayerData.GetPartLevel(iD);
		int maxLevel = Game.DataManager.GetPartData(iD).MaxLevel;
		levelLocaleText.SetDataFormat("UI_Part_Level", partLevel.ToString(), maxLevel.ToString());
		levelLocaleText.SetColor((partLevel < maxLevel) ? Color.white : new Color(1f, 0.5f, 0f));
		descLocaleText.SetData(LocaleKey.ItemEffect(iD));
		upgradeInfoLocaleText.SetData(LocaleKey.ItemUpgrade(iD));
		bool flag = CanProceedUpgrade() != UpgradeCheckResult.MaximumUpgrade;
		ShowRequirements(flag);
		if (flag)
		{
			UpdateCostText(Cost, UpgradeBoard<PartUpgradeModel>.PlayerStat.Money);
		}
	}

	public override UpgradeCheckResult CanProceedUpgrade()
	{
		if (currentModel?.ID == null)
		{
			return UpgradeCheckResult.None;
		}
		int maxLevel = Game.DataManager.GetPartData(currentModel.ID).MaxLevel;
		if (Game.Instance.SaveData.PlayerData.GetPartLevel(currentModel.ID) == maxLevel)
		{
			return UpgradeCheckResult.MaximumUpgrade;
		}
		int money = UpgradeBoard<PartUpgradeModel>.PlayerStat.Money;
		if (Cost > money)
		{
			return UpgradeCheckResult.NotEnoughOhn;
		}
		return UpgradeCheckResult.Success;
	}

	public override void OnProceedUpgrade()
	{
		UpgradeBoard<PartUpgradeModel>.PlayerStat.AddMoney(-Cost);
		int partLevel = Game.Instance.SaveData.PlayerData.GetPartLevel(currentModel.ID);
		Game.Instance.Player.PlayerPart.SetLevel(currentModel.ID, partLevel + 1);
		PlayParticle();
		UpdateUI();
		Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem");
	}

	public override void OnBlockUpgrade(UpgradeCheckResult ucResult)
	{
		if (ucResult == UpgradeCheckResult.MaximumUpgrade)
		{
			ShowRequirements(value: false);
			ShowWarning("UI_Upgrade_Warning_Part_Maximum");
		}
		else
		{
			UpdateFeedbackText(ucResult, onProceed: true);
		}
	}

	protected override void ShowRequirements(bool value)
	{
		costParent.SetActive(value);
	}

	private void PlayParticle()
	{
		equipParticle.Play();
	}
}
