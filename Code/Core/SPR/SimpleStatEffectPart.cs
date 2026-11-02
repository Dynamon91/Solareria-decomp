namespace SPR;

public abstract class SimpleStatEffectPart : Part
{
	private StatEffect statEffect;

	public override void OnEquip()
	{
		statEffect = new StatEffect(base.PartData.PartEffectDatasForEffects[0], base.Level);
		statEffect.Apply();
	}

	public override void Reset()
	{
	}

	public override void OnUnequip()
	{
		statEffect.Clear();
		statEffect = null;
	}
}
