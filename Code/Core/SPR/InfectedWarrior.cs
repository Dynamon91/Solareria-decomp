using System;
using System.Collections.Generic;
using Library;
using UnityEngine;

namespace SPR;

public class InfectedWarrior : Creature
{
	private enum Pattern
	{
		None,
		Attack1A,
		Attack1B,
		Attack2,
		Attack3
	}

	[Header("패턴 디버깅")]
	[SerializeField]
	private Pattern patternDebug;

	[Header("패턴")]
	[SerializeField]
	private GenericAttackSequence<InfectedWarrior> attack1;

	[SerializeField]
	private GenericAttackSequence<InfectedWarrior> attack1A;

	[SerializeField]
	private GenericAttackSequence<InfectedWarrior> attack1B;

	[SerializeField]
	private GenericAttackSequence<InfectedWarrior> attack2;

	[SerializeField]
	private GenericAttackSequence<InfectedWarrior> attack3;

	[Header("Attack 2")]
	[SerializeField]
	private int count;

	[SerializeField]
	private float minPower;

	[SerializeField]
	private float maxPower;

	[SerializeField]
	private int maxAngle;

	[SerializeField]
	private float gravity;

	[SerializeField]
	private float kickPower;

	public override string Name => "InfectedWarrior";

	public override string GroggyStartAnim => "groggy";

	public override string GroggyLoopAnim => "groggy";

	public override string GroggyEndAnim => "groggy_recovery";

	protected override void Awake()
	{
		base.Awake();
		TimelineActions.Add("SpreadFragments", delegate
		{
			SpreadFragments();
		});
		TimelineActions.Add("KickStone", delegate
		{
			KickStone();
		});
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack1, attack1A, attack1B, attack2, attack3 };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		if (patternDebug != Pattern.None)
		{
			return patternDebug switch
			{
				Pattern.Attack1A => new List<AttackSequence> { attack1, attack1A }, 
				Pattern.Attack1B => new List<AttackSequence> { attack1, attack1B }, 
				Pattern.Attack2 => new List<AttackSequence> { attack2 }, 
				Pattern.Attack3 => new List<AttackSequence> { attack3 }, 
				_ => throw new ArgumentOutOfRangeException(), 
			};
		}
		List<AttackSequence> list = new List<AttackSequence>();
		switch (CryptoRandom.Range(0, 3))
		{
		case 0:
			list.Add(attack1);
			list.Add(CryptoRandom.RandomBool() ? attack1A : attack1B);
			break;
		case 1:
			list.Add(attack2);
			break;
		case 2:
			list.Add(attack3);
			break;
		}
		return list;
	}

	private void SpreadFragments()
	{
		for (int i = 0; i < count; i++)
		{
			float power = UnityEngine.Random.Range(minPower, maxPower);
			Vector2 direction = BasicExtension.ToDirectionVector(CryptoRandom.Range(maxAngle, 180 - maxAngle));
			Vector2 vector = VisualBottom + new Vector2(0f, Bounds.extents.y / 2f);
			Enemy.PoolManager.GetObject<InfectedWarriorFragment>("Infected Warrior Fragment.prefab", vector).Initialize(direction, power, gravity, kick: false);
		}
	}

	private void KickStone()
	{
		Vector3 boneWorldPosition = GetBoneWorldPosition("Leg_R_2");
		InfectedWarriorFragment infectedWarriorFragment = Enemy.PoolManager.GetObject<InfectedWarriorFragment>("Infected Warrior Fragment.prefab", boneWorldPosition);
		Vector2 normalized = (Enemy.Player.VisualBottom + new Vector2(0f, Enemy.Player.Bounds.extents.y) - (Vector2)boneWorldPosition).normalized;
		infectedWarriorFragment.Initialize(normalized, kickPower, 0f, kick: true);
	}
}
