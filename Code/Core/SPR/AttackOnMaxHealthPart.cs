namespace SPR;

public class AttackOnMaxHealthPart : SimpleStatEffectPart, IHealthChangeEvent
{
	private StatEffect attackStatEffect;

	public override void OnEquip()
	{
		base.OnEquip();
		PartEffectData data = base.PartData.PartEffectDatasForEffects[1];
		attackStatEffect = new StatEffect(data, base.Level);
		TryApply();
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		attackStatEffect.Clear();
		attackStatEffect = null;
	}

	public override void Reset()
	{
		attackStatEffect.Clear();
	}

	public void OnHealthChanged()
	{
		TryApply();
	}

	private void TryApply()
	{
		if (Part.Player.PlayerStat.GetStat<Health>().CurrentHealth < Part.Player.PlayerStat.GetStat<Health>().MaxHealth)
		{
			attackStatEffect.Clear();
		}
		else if (!attackStatEffect.IsApplied)
		{
			attackStatEffect.Apply();
		}
	}
}
