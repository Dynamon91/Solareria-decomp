using System.Collections.Generic;
using Library;
using UnityEngine;

namespace SPR;

public class StalactiteGolem : Creature
{
	[SerializeField]
	private int spawnStalactiteChance;

	[SerializeField]
	private GenericAttackSequence<StalactiteGolem> stab;

	public override string Name => "StalactiteGolem";

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (CryptoRandom.Range(1, 101) < spawnStalactiteChance)
		{
			Enemy.PoolManager.GetObject<Stalactite>("Stalactite.prefab", VisualCenter);
		}
		return base.TryTakeDamage(hitInfo);
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { stab };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence> { stab };
	}
}
