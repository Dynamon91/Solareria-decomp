using System.Collections.Generic;
using UnityEngine;

namespace SPR.UI.Trait;

public class TraitStatusInfoBoard : TraitInfoBoard
{
	[SerializeField]
	private TraitStatusBasicInfoBoard basicInfoBoard;

	[SerializeField]
	private List<TraitStatusPassiveBoard> passiveInfoBoards;

	public override void Initialize()
	{
	}

	protected override void OnSetActive(bool value, bool instant)
	{
	}

	public override void SetInfo(TraitCardElement card, bool isRest, int point)
	{
		TraitStatusCardElement traitStatusCardElement = (TraitStatusCardElement)card;
		basicInfoBoard.SetInfo(traitStatusCardElement, isRest, point);
		for (int i = 0; i < passiveInfoBoards.Count; i++)
		{
			int num = i + 2;
			if (traitStatusCardElement.Rank >= num)
			{
				passiveInfoBoards[i].gameObject.SetActive(value: true);
				passiveInfoBoards[i].SetData(traitStatusCardElement, num);
			}
			else
			{
				passiveInfoBoards[i].gameObject.SetActive(value: false);
			}
		}
	}

	public void PlayActiveAnimation(bool value)
	{
		basicInfoBoard.PlayActiveAnimation(value);
	}

	public void PlayUpgradeAnimation(bool increase)
	{
		basicInfoBoard.PlayUpgradeAnimation(increase);
	}
}
