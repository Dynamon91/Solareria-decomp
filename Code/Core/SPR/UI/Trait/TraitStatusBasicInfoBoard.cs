using SPR.L10N;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Trait;

public class TraitStatusBasicInfoBoard : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI levelText;

	[SerializeField]
	private LocaleText nameText;

	[SerializeField]
	private Image iconImage;

	[SerializeField]
	private TraitRankIcon rank1;

	[SerializeField]
	private TraitRankIcon rank2;

	[SerializeField]
	private TraitRankIcon rank3;

	[SerializeField]
	private GameObject upArrow;

	[SerializeField]
	private GameObject downArrow;

	[SerializeField]
	private GameObject centerWhite;

	private Animator animator;

	private static readonly int Open = Animator.StringToHash("Open");

	private static readonly int Close = Animator.StringToHash("Close");

	private static readonly int Upgrade = Animator.StringToHash("Upgrade");

	private static readonly int Downgrade = Animator.StringToHash("Downgrade");

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	public void SetInfo(TraitStatusCardElement statusCard, bool isRest, int point)
	{
		TraitData traitData = Game.DataManager.GetTraitData(statusCard.ID);
		int differedLevel = statusCard.DifferedLevel;
		int maxLevel = traitData.MaxLevel;
		int rank = statusCard.Rank;
		bool flag = rank > 0;
		levelText.text = (flag ? statusCard.CurrentText : string.Empty);
		nameText.SetData(LocaleKey.ItemName(traitData.Name));
		iconImage.sprite = AddressableHelper.GetAsset<Sprite>("CoreStone", traitData.Name);
		rank1.SetActive(rank >= 1);
		rank2.SetActive(rank >= 2);
		rank3.SetActive(rank >= 3);
		if (isRest)
		{
			upArrow.SetActive(differedLevel < maxLevel && point > 0);
			downArrow.SetActive(differedLevel > 0);
			centerWhite.SetActive(value: true);
		}
		else
		{
			upArrow.SetActive(value: false);
			downArrow.SetActive(value: false);
			centerWhite.SetActive(value: false);
		}
		PlayActiveAnimation(flag);
	}

	public void PlayActiveAnimation(bool value)
	{
		int trigger = (value ? Open : Close);
		int id = (value ? Close : Open);
		Animator.ResetTrigger(Upgrade);
		Animator.ResetTrigger(Downgrade);
		Animator.ResetTrigger(id);
		Animator.SetTrigger(trigger);
	}

	public void PlayUpgradeAnimation(bool increase)
	{
		int trigger = (increase ? Upgrade : Downgrade);
		int id = (increase ? Downgrade : Upgrade);
		Animator.ResetTrigger(id);
		Animator.SetTrigger(trigger);
	}
}
