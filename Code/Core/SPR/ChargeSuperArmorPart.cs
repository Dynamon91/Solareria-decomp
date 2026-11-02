using UnityEngine;

namespace SPR;

public class ChargeSuperArmorPart : SimpleStatEffectPart, IDamagedEvent, IChargeAttackEvent
{
	private StatEffect counterStatEffect;

	private bool isCharging;

	private float rate;

	private float maxRate;

	public override void OnEquip()
	{
		base.OnEquip();
		PartEffectData partEffectData = base.PartData.PartEffectDatasForEffects[1];
		rate = partEffectData.Rates[base.Level] / 100f;
		maxRate = partEffectData.MaxValues[base.Level] / 100f;
		counterStatEffect = new StatEffect(base.PartData.PartEffectDatasForEffects[2], base.Level);
		counterStatEffect.Apply();
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		counterStatEffect.Clear();
		counterStatEffect = null;
	}

	public void OnChargeStart()
	{
		isCharging = true;
		Part.PlayerMover.PSM.SetStateOptionActive(StateOption.SuperArmor, active: true);
	}

	public void OnChargeEndOrCancel()
	{
		isCharging = false;
		Part.PlayerMover.PSM.SetStateOptionActive(StateOption.SuperArmor, active: false);
	}

	public override void Reset()
	{
		isCharging = false;
		Part.PlayerMover.PSM.SetStateOptionActive(StateOption.SuperArmor, active: false);
	}

	public void OnDamaged(IAttackSource source, int damage)
	{
		if (isCharging && source is Enemy enemy)
		{
			HeatFlame.ShootFlame(new HeatFlame.FlameParameter(enemy, 0));
		}
	}

	public float GetDamageAdditionalRate(float staminaUsage)
	{
		return Mathf.Min(staminaUsage * rate, maxRate);
	}
}
