using UnityEngine;

namespace SPR.UI.Trait;

public class TraitSkillCardElement : TraitCardElement
{
	[SerializeField]
	private GameObject frontObject;

	[SerializeField]
	private GameObject backObject;

	public bool Acquired { get; private set; }

	public override void RefreshData()
	{
		Acquired = TraitCardElement.PlayerData.GetCardLevel(base.ID) >= 0;
		SetIconImage(frontObject);
		frontObject.SetActive(Acquired);
		backObject.SetActive(!Acquired);
	}
}
