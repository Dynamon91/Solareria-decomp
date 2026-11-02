using SPR.L10N;
using UnityEngine;

namespace SPR.UI.Trait;

public class TraitStatusPassiveBoard : MonoBehaviour
{
	[SerializeField]
	private LocaleText descText;

	public void SetData(TraitStatusCardElement statusCard, int rank)
	{
		string coreStoneName = Game.DataManager.GetTraitData(statusCard.ID).Name;
		descText.SetData(LocaleKey.CoreStonePassiveDesc(coreStoneName, rank));
	}
}
