using System.Collections.Generic;
using UnityEngine;

namespace SPR;

public class Stony : Creature
{
	[SerializeField]
	private bool isMini;

	[Header("Attack")]
	[SerializeField]
	private GenericAttackSequence<Stony> attack;

	public override string Name
	{
		get
		{
			if (!isMini)
			{
				return "Stony";
			}
			return "MiniStony";
		}
	}

	public override string ID => Name;

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AttackPatterns.TryAdd(attack, () => new List<AttackSequence> { attack });
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack };
	}
}
