using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SPR.UI.Menu.Status;

public class HeatCoreInfoBoard : MonoBehaviour
{
	[SerializeField]
	private HeatDetailInfoIcon icon;

	[SerializeField]
	private LocaleText coreTitleLocaleText;

	[SerializeField]
	private LocaleText coreDescLocaleText;

	[SerializeField]
	private List<HeatSkillButton> buttons;

	private TextMeshProUGUI coreDescTMP;

	private TextMeshProUGUI CoreDescTMP => coreDescTMP ?? (coreDescTMP = coreDescLocaleText.GetComponent<TextMeshProUGUI>());

	public void SetData(int targetLevel, Dictionary<string, Sprite> sprites)
	{
		Sprite value = null;
		bool flag = sprites != null;
		bool flag2 = flag && sprites.TryGetValue("Lock", out value);
		if (!flag)
		{
			Debug.LogWarning("[HeatCoreInfoBoard] sprites가 null입니다. 잠금 상태로 표시합니다.");
		}
		else if (!flag2)
		{
			Debug.LogWarning("[HeatCoreInfoBoard] Lock 스프라이트가 없습니다. 버튼을 비활성 상태로 표시합니다.");
		}
		foreach (HeatSkillButton button in buttons)
		{
			string text = button.SkillType.ToString();
			Sprite sprite = null;
			if (flag && sprites.TryGetValue(text, out var value2))
			{
				sprite = value2;
			}
			else
			{
				Debug.LogWarning("[HeatCoreInfoBoard] " + text + " 스프라이트가 없습니다. Lock 스프라이트로 대체합니다.");
			}
			button.SetData(sprite ?? value, value);
		}
		if (targetLevel <= 0)
		{
			icon.SetInactiveState();
			coreTitleLocaleText.gameObject.SetActive(value: false);
			coreDescLocaleText.SetData("UI_HeatCore_Lock");
			CoreDescTMP.color = Color.gray;
		}
		else
		{
			icon.SetLevel(targetLevel);
			coreTitleLocaleText.gameObject.SetActive(value: true);
			coreTitleLocaleText.SetData("UI_HeatCore_Name");
			coreDescLocaleText.SetData("UI_HeatCore_Desc");
			CoreDescTMP.color = Color.white;
		}
	}

	public bool TryGetSkillButton(int xIndex, out HeatSkillButton skillButton)
	{
		skillButton = null;
		if (xIndex < 0 || xIndex > 3)
		{
			return false;
		}
		HeatSkillButton heatSkillButton = buttons[xIndex];
		if (!heatSkillButton.Active)
		{
			return false;
		}
		skillButton = heatSkillButton;
		return true;
	}
}
