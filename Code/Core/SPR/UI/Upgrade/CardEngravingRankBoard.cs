using System.Collections.Generic;
using System.Linq;
using System.Text;
using SPR.L10N;
using SPR.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Upgrade;

public class CardEngravingRankBoard : MonoBehaviour
{
	[SerializeField]
	private LocaleText rankTitleText;

	[SerializeField]
	private LocaleText rankDescText;

	[SerializeField]
	private Image rankIconFillImage;

	public void SetData(CardData cardData, int myRank)
	{
		rankTitleText.SetData(LocaleKey.CoreStoneEngravingTitle(myRank));
		int rank = cardData.Rank;
		rankIconFillImage.gameObject.SetActive(myRank <= rank);
		if (myRank == 1)
		{
			rankDescText.SetRawText(GetBasicEngravingDesc(cardData));
			return;
		}
		string coreStoneName = Game.DataManager.GetTraitData(cardData.ID).Name;
		rankDescText.SetData(LocaleKey.CoreStoneEngravingDesc(coreStoneName, myRank));
	}

	private string GetBasicEngravingDesc(CardData cardData)
	{
		IEnumerable<StatType> enumerable = (from tuple in StatUtility.GetTraitStatIncreasesByLevel(cardData.ID, 1, 1).Select(((StatType statType, float increases) tuple, int i) => (statType: tuple.statType, i: i))
			orderby (tuple.statType == StatType.Defense) ? 24 : tuple.i
			select tuple.statType).Distinct();
		StringBuilder stringBuilder = new StringBuilder();
		foreach (StatType item in enumerable)
		{
			string localeString = Locale.GetLocaleString(LocaleKey.StatName(item));
			stringBuilder.Append(localeString + "\n");
		}
		return stringBuilder.ToString();
	}
}
