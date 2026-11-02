namespace SPR;

public class RemoveDebuffPart : WarmhornPart
{
	public override void OnHeal()
	{
		Part.Player.StatusEffectController.Clear();
	}
}
