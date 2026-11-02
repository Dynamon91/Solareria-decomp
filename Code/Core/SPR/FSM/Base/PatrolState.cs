using System;
using System.Collections.Generic;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR.FSM.Base;

public class PatrolState : CreatureState
{
	protected RandomFrameTimer Timer;

	protected Vector2 TargetPosition;

	protected bool ShouldWait;

	private readonly int min;

	private readonly int max;

	protected Stack<PathFinder.Node> Path = new Stack<PathFinder.Node>();

	protected int Min => Mathf.RoundToInt((float)min * (1f - (base.Creature.SpeedScale - MathF.Truncate(base.Creature.SpeedScale))));

	protected int Max => Mathf.RoundToInt((float)max * (1f - (base.Creature.SpeedScale - MathF.Truncate(base.Creature.SpeedScale))));

	public PatrolState(Creature creature, int minFrame, int maxFrame)
		: base(creature)
	{
		min = minFrame;
		max = maxFrame;
	}

	public override void Enter(IState prevState)
	{
		ShouldWait = !base.Creature.CanMove || CryptoRandom.RandomBool();
		if (ShouldWait)
		{
			base.Creature.SetAnimation(base.Creature.IdleAnim);
			Timer = new RandomFrameTimer(Min, Max);
		}
		else
		{
			base.Creature.SetAnimation(base.Creature.WalkFrontAnim);
			SetPath();
		}
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (base.Creature is HighCreature highCreature)
		{
			if (base.Creature.IsPlayerInSightBound())
			{
				if (highCreature.FatigueIsDone && base.Creature.ShouldChangeToAttackState())
				{
					ChangeState(base.Creature.AttackState);
					return;
				}
				if (base.Creature.CanReach(EnemyState.Player.GetPositionFromGroud()))
				{
					ChangeState(base.Creature.ChaseState);
					return;
				}
			}
		}
		else if (base.Creature.IsPlayerInSightBound() && TrySelect())
		{
			return;
		}
		if (ShouldWait)
		{
			Timer.Tick();
			if (Timer.IsDone)
			{
				ChangeState(base.Creature.PatrolState);
			}
		}
		else
		{
			Patrol();
		}
	}

	protected void SetPath()
	{
		TargetPosition = base.Creature.GetPatrolDestination();
		Path = PathFinder.FindPath(base.Creature.transform.position, TargetPosition);
		base.Creature.PathDebug.Clear();
		foreach (PathFinder.Node item in Path)
		{
			base.Creature.PathDebug.Add(item);
		}
	}

	protected void Patrol()
	{
		if (Path.Count == 0)
		{
			ChangeState(base.Creature.PatrolState);
			return;
		}
		Vector2 position = Path.Peek().Position;
		base.Creature.LookAt(position);
		base.Creature.Move(position);
		if (Vector2.Distance(position, base.Creature.transform.position) <= base.Creature.MoveForwardDelta)
		{
			Path.Pop();
		}
	}

	private bool TrySelect()
	{
		List<AttackSequence> attackSequences = base.Creature.GetAttackSequences();
		if (attackSequences == null)
		{
			base.Creature.ResetChance();
			return false;
		}
		ChangeState(new ChaseState(base.Creature, attackSequences));
		return true;
	}
}
