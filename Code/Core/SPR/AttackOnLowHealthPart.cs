namespace SPR;

public class AttackOnLowHealthPart : SimpleStatEffectPart, IHealthChangeEvent
{
	private StatEffect statEffect;

	private float healthRate;

	public override void OnEquip()
	{
		base.OnEquip();
		PartEffectData partEffectData = base.PartData.PartEffectDatasForEffects[1];
		statEffect = new StatEffect(partEffectData, base.Level);
		healthRate = partEffectData.Conditions[base.Level] / 100f;
		UpdateAttack();
	}

	public override void Reset()
	{
		statEffect.Clear();
	}

	public void OnHealthChanged()
	{
		UpdateAttack();
	}

	private void UpdateAttack()
	{
		statEffect.Clear();
		Health stat = Game.Instance.Player.PlayerStat.GetStat<Health>();
		for (float num = 1f - (float)stat.CurrentHealth / (float)stat.MaxHealth; num > healthRate; num -= healthRate)
		{
			statEffect.Apply();
		}
	}
}
