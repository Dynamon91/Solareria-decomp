using System.Collections.Generic;
using UnityEngine;

namespace SPR;

public class DummyAttack : Dummy
{
	[SerializeField]
	private GenericAttackSequence<DummyAttack> attack;

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AddAttack(attack, () => new List<AttackSequence> { attack });
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack };
	}
}
