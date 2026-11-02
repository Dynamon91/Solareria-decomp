using System.Collections.Generic;
using UnityEngine;

namespace SPR;

public class Spearman : Creature
{
	[Header("Attack Sequence")]
	[SerializeField]
	private GenericAttackSequence<Spearman> thrust;

	public override string Name => "Spearman";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AttackPatterns.TryAdd(thrust, () => new List<AttackSequence> { thrust });
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { thrust };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence> { thrust };
	}
}
