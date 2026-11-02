using System.Collections.Generic;
using UnityEngine;

namespace SPR;

public class InfectedMucoraSwordsman : Creature
{
	[Header("Sequences")]
	[SerializeField]
	private GenericAttackSequence<InfectedMucoraSwordsman> highSpeedSlash;

	[SerializeField]
	private GenericAttackSequence<InfectedMucoraSwordsman> squareSlash;

	public override string Name => "InfectedMucoraSwordsman";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AttackPatterns.TryAdd(highSpeedSlash, () => new List<AttackSequence> { highSpeedSlash });
		AttackPatterns.TryAdd(squareSlash, () => new List<AttackSequence> { squareSlash });
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { highSpeedSlash, squareSlash };
	}

	public override List<AttackSequence> GetAttackSequences()
	{
		return base.GetAttackSequences();
	}
}
