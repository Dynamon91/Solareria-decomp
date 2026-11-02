using System.Collections.Generic;
using UnityEngine;

namespace SPR;

public class InfectedMucoraWarrior : Creature
{
	[Header("Sequences")]
	[SerializeField]
	private GenericAttackSequence<InfectedMucoraWarrior> strike;

	[SerializeField]
	private GenericAttackSequence<InfectedMucoraWarrior> mucoraStyle;

	[Header("References")]
	[SerializeField]
	private Transform smallMuconSpawn;

	public override string Name => "InfectedMucoraWarrior";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		BeginParamActions.Add("SpawnSmallMucon", delegate(int _, Parameter[] p)
		{
			SpawnSmallMucon(p[0].Int);
		});
		AttackPatterns.TryAdd(strike, () => new List<AttackSequence> { strike });
		AttackPatterns.TryAdd(mucoraStyle, () => new List<AttackSequence> { mucoraStyle });
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { strike, mucoraStyle };
	}

	public override List<AttackSequence> GetAttackSequences()
	{
		return base.GetAttackSequences();
	}

	private void SpawnSmallMucon(int count)
	{
		for (int i = 0; i < count; i++)
		{
			Enemy.PoolManager.GetObject<SmallMucon>("Small Mucon.prefab", smallMuconSpawn.position).Spread();
		}
	}
}
