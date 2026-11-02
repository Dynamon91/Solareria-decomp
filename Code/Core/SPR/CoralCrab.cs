using System.Collections.Generic;
using Library;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class CoralCrab : Creature
{
	[SerializeField]
	private int armorMaxHealth;

	[SerializeField]
	private int defaultDefense;

	[SerializeField]
	private int armoredDefense;

	[SerializeField]
	private int armorBrokenSpeed;

	[SerializeField]
	private int gasDuration;

	[SerializeField]
	private int gasInterval;

	[SerializeField]
	private GenericAttackSequence<CoralCrab> gas;

	[SerializeField]
	private GenericAttackSequence<CoralCrab> swing;

	private int armorHealth;

	private bool armorBreak;

	public override string Name => "CoralCrab";

	protected override int OriginDef
	{
		get
		{
			if (armorHealth <= 0)
			{
				return defaultDefense;
			}
			return armoredDefense;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		armorHealth = armorMaxHealth;
		TimelineActions.Add("Exhaling Gas", delegate
		{
			ExhalingGas();
		});
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (armorHealth > 0)
		{
			armorHealth -= hitInfo.Damage;
		}
		if (!armorBreak && armorHealth <= 0)
		{
			base.SkeletonAnimation.skeleton.SetColor(Color.red);
			base.SpeedScale = 1f + (float)armorBrokenSpeed / 100f;
			base.VfxController.SetSpeedAll(base.SpeedScale);
			armorBreak = true;
		}
		return base.TryTakeDamage(hitInfo);
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { gas, swing };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence> { base.AllAttackSequences[CryptoRandom.Range(0, base.AllAttackSequences.Count)] };
	}

	private void ExhalingGas()
	{
		CoralCrabGas coralCrabGas = Enemy.PoolManager.GetObject<CoralCrabGas>("Coral Crab Gas.prefab", VisualBottom);
		Vector3 localScale = coralCrabGas.transform.localScale;
		coralCrabGas.transform.position += new Vector3((float)FacingDirection.GetSignX() * localScale.x, localScale.y / 2f);
		coralCrabGas.Initialize(gasDuration, gasInterval);
	}
}
