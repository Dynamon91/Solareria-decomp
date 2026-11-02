using System.Collections.Generic;
using Library;
using UnityEngine;

namespace SPR;

public class WaterWarrior : Creature
{
	[Header("백어택")]
	[SerializeField]
	private float backAttackDistance;

	[Header("Sequences")]
	[SerializeField]
	private GenericAttackSequence<WaterWarrior> attack;

	[SerializeField]
	private GenericAttackSequence<WaterWarrior> backAttack;

	public override string Name => "WaterWarrior";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AttackPatterns.TryAdd(attack, () => new List<AttackSequence> { attack });
		AttackPatterns.TryAdd(backAttack, () => new List<AttackSequence> { backAttack });
		TimelineActions.Add("BackAttack Jump", delegate
		{
			BackAttackTeleport();
		});
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack, backAttack };
	}

	private void BackAttackTeleport()
	{
		int sign = (Enemy.Player.VisualBottom.x - VisualBottom.x).GetSign();
		PathFinder.Node node = PathFinder.FindNode(Enemy.Player.GetPositionFromGroud());
		Vector2 position = PathFinder.FindNode(Enemy.Player.GetPositionFromGroud()).Shift(sign, (int)backAttackDistance).Position;
		if (PathFinder.FindNode(position) == null)
		{
			position = node.Position;
		}
		base.transform.position = position;
		SetConnectedPath();
	}
}
