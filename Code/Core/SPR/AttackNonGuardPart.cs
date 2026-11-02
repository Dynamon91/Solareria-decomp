namespace SPR;

public class AttackNonGuardPart : MultiStatEffectPart
{
	public override void OnEquip()
	{
		base.OnEquip();
		Part.PlayerMover.PSM.GuardInputItem.Active = false;
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		Part.PlayerMover.PSM.GuardInputItem.Active = true;
	}
}
