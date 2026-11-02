namespace SPR.UI.Trait;

public class TraitSkillCardBoard : TraitCardBoard
{
	protected override int FirstID => 1001;

	protected override bool UsingActivationEffect => true;

	public void ActivateCards(bool value)
	{
		foreach (TraitCardElement card in base.Cards)
		{
			card.gameObject.SetActive(value);
		}
	}
}
