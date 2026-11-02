using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR;

public class CoralKeeper : Creature
{
	[Header("Sequences")]
	[SerializeField]
	private GenericAttackSequence<CoralKeeper> tail;

	[Header("References")]
	[SerializeField]
	private ParticleSystem idleEffect;

	[SerializeField]
	private PlayableDirector idleSfxPlayer;

	public override string Name => "CoralKeeper";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AttackPatterns.TryAdd(tail, () => new List<AttackSequence> { tail });
	}

	protected override void OnDie()
	{
		base.OnDie();
		idleEffect.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
		idleSfxPlayer.Stop();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { tail };
	}
}
