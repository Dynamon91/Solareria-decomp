using System;
using SPR.Library;
using UnityEngine;

namespace SPR.FSM.Base;

public class AttackWaitState : CreatureState
{
	protected RandomFrameTimer Timer;

	private readonly int min;

	private readonly int max;

	protected int Min => Mathf.RoundToInt((float)min * (1f - (base.Creature.SpeedScale - MathF.Truncate(base.Creature.SpeedScale))));

	protected int Max => Mathf.RoundToInt((float)max * (1f - (base.Creature.SpeedScale - MathF.Truncate(base.Creature.SpeedScale))));

	public AttackWaitState(Creature creature, int minFrame, int maxFrame)
		: base(creature)
	{
		min = minFrame;
		max = maxFrame;
	}

	public override void Enter(IState prevState)
	{
		base.Enemy.SetAnimation(base.Creature.IdleAnim);
		Timer = new RandomFrameTimer(Min, Max);
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		Timer.Tick();
		if (Timer.IsDone)
		{
			if (base.Creature.ShouldChangeToAttackState())
			{
				ChangeState(base.Creature.AttackState);
			}
			else if (!base.Creature.IsPlayerInSightBound())
			{
				ChangeState(base.Creature.DefaultState);
			}
			else if (base.Creature.CanReach(EnemyState.Player.GetPositionFromGroud()))
			{
				ChangeState(base.Creature.ChaseState);
			}
			else
			{
				ChangeState(base.Creature.DefaultState);
			}
		}
	}
}
