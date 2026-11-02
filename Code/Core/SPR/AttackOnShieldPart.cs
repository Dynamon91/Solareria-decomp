using System.Collections.Generic;

namespace SPR;

public class AttackOnShieldPart : SimpleStatEffectPart, IShieldDestroyedEvent
{
	private StatEffect statEffect;

	private const string SkillID = "wind_shield_destroyed";

	public override void OnEquip()
	{
		base.OnEquip();
		statEffect = new StatEffect(base.PartData.PartEffectDatasForEffects[1], base.Level);
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		statEffect.Clear();
		statEffect = null;
	}

	public override void OnFixedUpdate()
	{
		if (!statEffect.IsApplied && Part.PlayerMover.PlayerShield.Health > 0)
		{
			statEffect.Apply();
		}
		else if (statEffect.IsApplied && Part.PlayerMover.PlayerShield.Health <= 0)
		{
			statEffect.Clear();
		}
	}

	public override void Reset()
	{
		statEffect.Clear();
	}

	private void Attack()
	{
		Part.PlayerMover.HandleAttackHitBox("wind_shield_destroyed", 0, AttackAttribute.Default | AttackAttribute.BlockStuck, 0f, null, new List<IDamageable>());
		Part.PlayerMover.EnableVFX("wind_shield_destroyed");
	}

	public void OnShieldDestroyed(int blockedDamage)
	{
		Attack();
	}
}
