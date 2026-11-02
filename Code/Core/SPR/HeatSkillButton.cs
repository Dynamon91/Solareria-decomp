using SPR.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SPR;

public class HeatSkillButton : MonoBehaviour
{
	[SerializeField]
	private HeatSkillType skillType;

	private Image iconImage;

	private GameObject notiIcon;

	private LocaleText localeText;

	public HeatSkillType SkillType => skillType;

	public string SkillName => skillType.ToString();

	public bool Active { get; private set; }

	private void Awake()
	{
		iconImage = base.transform.GetChild(0).GetComponent<Image>();
		notiIcon = iconImage.transform.GetChild(0).gameObject;
		localeText = GetComponentInChildren<LocaleText>();
	}

	public void SetData(Sprite activeSprite, Sprite lockSprite)
	{
		Active = PlayerHeat.CanActivateSkill(skillType);
		iconImage.sprite = (Active ? activeSprite : lockSprite);
		iconImage.color = (Active ? Color.white : Color.gray);
		localeText.gameObject.SetActive(Active);
		localeText.SetData("Heat_" + SkillName + "_Name");
		bool flag = !Game.Instance.SaveData.WanderingData.IsWatchedHeatSkillType(SkillType);
		SetNotiIcon(Active && flag);
	}

	public void SetNotiIcon(bool active)
	{
		notiIcon.SetActive(active);
	}
}
