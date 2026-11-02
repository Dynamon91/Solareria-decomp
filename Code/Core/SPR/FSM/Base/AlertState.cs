using System;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR.FSM.Base;

public class AlertState : CreatureState
{
	protected RandomFrameTimer Timer;

	private readonly int min;

	private readonly int max;

	protected bool ShouldWait;

	protected bool MoveForward;

	private int direction;

	protected int Min => Mathf.RoundToInt((float)min * (1f - (base.Creature.SpeedScale - MathF.Truncate(base.Creature.SpeedScale))));

	protected int Max => Mathf.RoundToInt((float)max * (1f - (base.Creature.SpeedScale - MathF.Truncate(base.Creature.SpeedScale))));

	public AlertState(Creature creature, int minFrame, int maxFrame)
		: base(creature)
	{
		min = minFrame;
		max = maxFrame;
	}

	public override void Enter(IState prevState)
	{
		Timer = new RandomFrameTimer(Min, Max);
		ShouldWait = !base.Creature.CanMove || CryptoRandom.RandomBool();
		if (ShouldWait)
		{
			base.Creature.SetAnimation(base.Creature.IdleAnim);
			return;
		}
		base.Creature.LookPlayer();
		direction = base.Creature.FacingDirection.GetSignX();
		MoveForward = base.Creature.OnlyWalkFront || CryptoRandom.RandomBool();
		base.Creature.SetAnimation(MoveForward ? base.Creature.WalkFrontAnim : base.Creature.WalkBackAnim);
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (base.Creature.CanMove)
		{
			base.Creature.LookPlayer();
		}
		int signX = base.Creature.FacingDirection.GetSignX();
		Timer.Tick();
		if (!ShouldWait)
		{
			if (MoveForward && signX != direction)
			{
				MoveForward = !MoveForward;
				base.Creature.SetAnimation(base.Creature.WalkBackAnim);
			}
			float velocityX = (MoveForward ? base.Creature.MoveForwardSpeed : base.Creature.MoveBackSpeed);
			base.Creature.MoveX(velocityX, fdt, MoveForward);
		}
		if (Timer.IsDone)
		{
			ChangeState(base.Creature.DefaultState);
		}
	}
}
