using System.Collections.Generic;
using Library;
using UnityEngine;

namespace SPR;

public class Stoner : Creature
{
	[SerializeField]
	private GenericAttackSequence<Stoner> attack;

	[SerializeField]
	private GenericAttackSequence<Stoner> attack2;

	public override string Name => "Stoner";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AttackPatterns.TryAdd(attack, () => new List<AttackSequence> { attack });
		AttackPatterns.TryAdd(attack2, () => new List<AttackSequence> { attack2 });
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack, attack2 };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		List<AttackSequence> list = new List<AttackSequence>();
		if (IsPlayerInAttackBound(attack))
		{
			list.Add(attack);
		}
		if (IsPlayerInAttackBound(attack2))
		{
			list.Add(attack2);
		}
		return new List<AttackSequence> { list[CryptoRandom.Range(0, list.Count)] };
	}
}
