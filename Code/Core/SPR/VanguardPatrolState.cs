using SPR.FSM;
using SPR.FSM.Base;
using UnityEngine;

namespace SPR;

public class VanguardPatrolState : PatrolState
{
	public VanguardPatrolState(Creature creature)
		: base(creature, 0, 0)
	{
	}

	public override void Enter(IState prevState)
	{
		base.Creature.SetAnimation(base.Creature.IdleAnim);
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (base.Creature.ShouldChangeToAttackState())
		{
			ChangeState(base.Creature.AttackState);
		}
		else if (base.Creature.IsPlayerInSightBound() && base.Creature.CanReach(EnemyState.Player.GetPositionFromGroud()))
		{
			ChangeState(base.Creature.ChaseState);
		}
		else
		{
			if (base.Creature.VisualBottom == base.Creature.SpawnPos)
			{
				return;
			}
			base.Creature.SetAnimation(base.Creature.WalkFrontAnim);
			if (Path.Count == 0)
			{
				ChangeState(base.Creature.PatrolState);
				return;
			}
			base.Creature.LookAt(Path.Peek().Position);
			base.Creature.Move(Path.Peek().Position);
			if (Vector2.Distance(Path.Peek().Position, base.Creature.VisualBottom) <= base.Creature.MoveForwardDelta)
			{
				Path.Pop();
			}
		}
	}
}
