using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

namespace SPR;

public class TwistedFireSpirit : Creature
{
	[Header("공격 패턴")]
	[SerializeField]
	private GenericAttackSequence<TwistedFireSpirit> swingAttack;

	[SerializeField]
	private GenericAttackSequence<TwistedFireSpirit> walkAttack;

	[Header("Cinemachine Impulse")]
	[SerializeField]
	private float force;

	private CinemachineImpulseSource impulseSource;

	private CinemachineImpulseSource ImpulseSource => impulseSource ?? (impulseSource = GetComponent<CinemachineImpulseSource>());

	public override string Name => "TwistedFireSpirit";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		TimelineBeginActions.Add("Impulse", delegate
		{
			ImpulseSource.GenerateImpulse(force);
		});
		AddAttack(swingAttack, () => new List<AttackSequence> { swingAttack });
		AddAttack(walkAttack, () => new List<AttackSequence> { walkAttack });
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { swingAttack, walkAttack };
	}
}
