using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SPR;

public class WaterKnight : Creature
{
	[Header("버티기")]
	[SerializeField]
	private float holdChance;

	[Header("Sequence")]
	[SerializeField]
	private GenericAttackSequence<WaterKnight> swing;

	[SerializeField]
	private GenericAttackSequence<WaterKnight> swingAndThrust;

	[SerializeField]
	private GenericAttackSequence<WaterKnight> hold;

	private bool isHolding;

	public override string Name => "WaterKnight";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AttackPatterns.TryAdd(swing, () => new List<AttackSequence> { swing });
		AttackPatterns.TryAdd(swingAndThrust, () => new List<AttackSequence> { swingAndThrust });
		AttackPatterns.TryAdd(hold, () => new List<AttackSequence> { hold });
		AddAction(ActionTiming.Begin, "Holding", (Action)delegate
		{
			isHolding = true;
		});
		AddAction(ActionTiming.End, "Holding", (Action)delegate
		{
			isHolding = false;
		});
		AddAction(ActionTiming.Begin, "FallingAmbush", FallingAmbush);
	}

	public override List<AttackSequence> GetAttackSequences()
	{
		return base.GetAttackSequences();
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (isHolding && hitInfo.AttackAttribute.HasFlag(AttackAttribute.HeavyAttack))
		{
			base.StateMachine.ChangeState(base.AlertState);
		}
		return base.TryTakeDamage(hitInfo);
	}

	public override void ResetChance()
	{
		float value = (100f - holdChance) / (float)AttackChance.Count - 1f;
		AttackChance[swing] = value;
		AttackChance[swingAndThrust] = value;
		AttackChance[hold] = holdChance;
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { swing, swingAndThrust, hold };
	}

	protected override void InitiateChance()
	{
		base.InitiateChance();
		float num = (100f - holdChance) / (float)AttackChance.Count - 1f;
		foreach (var (attackSequence2, _) in AttackPatterns)
		{
			AttackChance.TryAdd(attackSequence2, (attackSequence2 == hold) ? holdChance : num);
		}
	}

	private void FallingAmbush(int duration)
	{
		PathFinder.Node node = PathFinder.FindNode(Enemy.Player.GetPositionFromGroud());
		base.transform.DOJump(node.Position, 10f, 1, Util.FrameToSeconds(duration)).SetEase(Ease.Linear).OnComplete(base.SetConnectedPath);
	}
}
