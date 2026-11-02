namespace SPR;

public class ArmorOnHealPart : WarmhornPart
{
	private StatEffect statEffect;

	public override void OnEquip()
	{
		base.OnEquip();
		statEffect = new StatEffect(base.PartData.PartEffectDatasForEffects[1], base.Level);
	}

	public override void Reset()
	{
		Part.PlayerMover.PSM.SetStateOptionActive(StateOption.SuperArmor, active: false);
		statEffect.Clear();
	}

	public override void OnStartHeal()
	{
		Part.PlayerMover.PSM.SetStateOptionActive(StateOption.SuperArmor, active: true);
		statEffect.Apply();
	}

	public override void OnEndHeal()
	{
		Reset();
	}
}
