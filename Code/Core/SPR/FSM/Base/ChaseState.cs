using System.Collections.Generic;
using System.Linq;
using SPR.Library;
using UnityEngine;

namespace SPR.FSM.Base;

public class ChaseState : CreatureState
{
	protected Stack<PathFinder.Node> Path = new Stack<PathFinder.Node>();

	private readonly List<AttackSequence> attackSequences;

	private readonly FrameTimer chaseTimer;

	public ChaseState(Creature creature)
		: base(creature)
	{
	}

	public ChaseState(Creature creature, List<AttackSequence> attackSequences)
		: base(creature)
	{
		this.attackSequences = attackSequences;
		if (attackSequences[0].ChaseFrame == 0)
		{
			chaseTimer = null;
		}
		chaseTimer = new FrameTimer(attackSequences[0].ChaseFrame);
	}

	public override void Enter(IState prevState)
	{
		if (base.Creature.CanMove)
		{
			base.Creature.SetAnimation(base.Creature.WalkFrontAnim);
			SetPath(base.Creature.transform.position, base.Creature.ChaseNode?.Position ?? EnemyState.Player.GetPositionFromGroud());
		}
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (chaseTimer != null && chaseTimer.IsDone)
		{
			ChangeState(base.Creature.AlertState);
			return;
		}
		chaseTimer?.Tick();
		if (base.Creature is HighCreature highCreature)
		{
			if (highCreature.FatigueIsDone && base.Creature.ShouldChangeToAttackState())
			{
				ChangeState(base.Creature.AttackState);
				return;
			}
		}
		else
		{
			if (attackSequences[0].CheckPlatform && !base.Creature.CanReach(EnemyState.Player.GetPositionFromGroud()))
			{
				ChangeState(base.Creature.AlertState);
				return;
			}
			if (attackSequences[0].GetSightZone(base.Creature).Contains(EnemyState.Player.ColliderBounds))
			{
				ChangeState(new AttackState(base.Creature, () => attackSequences, forceAttack: true));
				return;
			}
		}
		if (!base.Creature.CanMove)
		{
			return;
		}
		if (Path.Count == 0)
		{
			if (!base.Creature.CanReach(EnemyState.Player.GetPositionFromGroud()))
			{
				ChangeState(base.Creature.AlertState);
			}
			else
			{
				SetPath(base.Creature.transform.position, EnemyState.Player.GetPositionFromGroud());
			}
		}
		else
		{
			Move();
		}
	}

	protected void SetPath(Vector2 start, Vector2 end)
	{
		Path = PathFinder.FindPath(start, end);
		base.Creature.PathDebug = Path.ToList();
		if (Path.Count != 0)
		{
			return;
		}
		if (attackSequences[0].GetSightZone(base.Creature).Contains(EnemyState.Player.ColliderBounds))
		{
			ChangeState(new AttackState(base.Creature, () => attackSequences, forceAttack: true));
		}
		else
		{
			ChangeState(base.Creature.DefaultState);
		}
	}

	protected void Move()
	{
		if (Path.Count == 0)
		{
			return;
		}
		base.Creature.LookAt(Path.Peek().Position);
		base.Creature.Move(Path.Peek().Position);
		if (Vector2.Distance(Path.Peek().Position, base.Creature.transform.position) <= base.Creature.MoveForwardDelta)
		{
			Path.Pop();
			if (!base.Creature.IsPlayerInSightBound() || !base.Creature.CanReach(EnemyState.Player.GetPositionFromGroud()) || !base.Creature.CanReach(EnemyState.Player.VisualBottom))
			{
				ChangeState(base.Creature.DefaultState);
			}
		}
	}
}
