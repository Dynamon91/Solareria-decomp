using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SPR.Save;
using SPR.UI.Menu.Status;
using UnityEngine;

namespace SPR.UI.Menu;

public class MenuStatusPanel : MenuPanel
{
	[Header("Board")]
	[SerializeField]
	private HeatCoreInfoBoard heatCoreInfoBoard;

	[SerializeField]
	private HeatSkillDetailInfoBoard skillDetailInfo;

	[SerializeField]
	private HeatSkillIndicator skillIndicator;

	[SerializeField]
	private StatusBoard statusBoard;

	[SerializeField]
	private StatusBoard rightStatusBoard;

	private int skillXIndex;

	private int skillYIndex;

	private Dictionary<string, Sprite> skillIconSprites;

	public override MenuCategory Category => MenuCategory.Status;

	private static ControlGuideGroup GuideGroup => Game.Instance.UIManager.MenuCanvas.GuideGroup;

	private static WanderingData WanderingData => Game.Instance.SaveData.WanderingData;

	private static int CurrentHeatMaxLevel => PlayerStat.HeatMaxLevel;

	public override void Initialize()
	{
		CacheSpriteAsync().Forget();
	}

	protected override void OnActivate()
	{
		Game.Instance.UIManager.TotFigure.SetActive(value: true);
		heatCoreInfoBoard.SetData(CurrentHeatMaxLevel, skillIconSprites);
		statusBoard.SetData();
		rightStatusBoard.SetData();
		skillIndicator.Initialize();
		skillDetailInfo.Initialize();
		skillXIndex = 0;
		if (Game.Instance.SaveData.PlayerData.CurrentDropData.IsValid)
		{
			statusBoard.SetDropStatus();
		}
	}

	protected override void OnControl()
	{
		int horizontalDownUI = MenuPanel.InputManager.horizontalDownUI;
		bool detail = MenuPanel.InputManager.detail;
		HeatSkillButton skillButton2;
		if (skillIndicator.IsActive)
		{
			if (!skillIndicator.CanControl)
			{
				return;
			}
			if (horizontalDownUI != 0)
			{
				int num = skillXIndex;
				num += horizontalDownUI;
				if (heatCoreInfoBoard.TryGetSkillButton(num, out var skillButton))
				{
					skillXIndex = num;
					skillIndicator.MoveAsync(horizontalDownUI).Forget();
					UpdateSkillDetailInfo(skillButton);
					MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_ViewStatus_Cursor");
				}
			}
			if (detail && skillDetailInfo.IsActive)
			{
				skillIndicator.SetActive(value: false);
				skillDetailInfo.SetActive(value: false);
				MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Confirm");
				MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem_Hide");
			}
		}
		else if (detail && heatCoreInfoBoard.TryGetSkillButton(0, out skillButton2))
		{
			skillIndicator.SetActive(value: true);
			skillDetailInfo.SetActive(value: true);
			UpdateSkillDetailInfo(skillButton2);
			MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_Cursor_Confirm");
			MenuPanel.SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_GetItem_Show");
		}
		void UpdateSkillDetailInfo(HeatSkillButton button)
		{
			skillDetailInfo.SetData(button.SkillType);
			WanderingData.AddWatchedHeatSkillType(button.SkillType);
			button.SetNotiIcon(active: false);
		}
	}

	protected override void OnDeactivate()
	{
		Game.Instance.UIManager.TotFigure.SetActive(value: false);
	}

	private async UniTask CacheSpriteAsync()
	{
		Dictionary<string, Sprite> dictionary = skillIconSprites;
		if (dictionary != null && dictionary.Count == 5)
		{
			return;
		}
		skillIconSprites = new Dictionary<string, Sprite>();
		for (int i = 1; i < 5; i++)
		{
			HeatSkillType heatSkillType = (HeatSkillType)i;
			string iconID = heatSkillType.ToString();
			if (!skillIconSprites.ContainsKey(iconID))
			{
				Sprite value = await AddressableHelper.GetSpriteAsync(Address.IconSprite(MenuCategory.Status, iconID));
				skillIconSprites[iconID] = value;
			}
		}
		if (!skillIconSprites.ContainsKey("Lock"))
		{
			Sprite value2 = await AddressableHelper.GetSpriteAsync(Address.IconSprite(MenuCategory.Status, "Lock"));
			skillIconSprites["Lock"] = value2;
		}
	}
}
