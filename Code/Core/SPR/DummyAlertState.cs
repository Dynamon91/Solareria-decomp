using System.Collections.Generic;
using Library;
using SPR.FSM.Base;
using SPR.Library;

namespace SPR;

public class DummyAlertState : CreatureState
{
	private readonly Dummy dummy;

	private readonly RandomFrameTimer timer;

	private bool wait;

	public DummyAlertState(Dummy enemy, int min, int max)
		: base(enemy)
	{
		dummy = enemy;
		timer = new RandomFrameTimer(min, max);
	}

	public override void Enter(IState prevState)
	{
		wait = CryptoRandom.RandomBool();
		base.Creature.SetAnimation(dummy.IdleAnim);
		if (wait)
		{
			timer.Reset();
		}
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (wait)
		{
			if (timer.IsDone)
			{
				ChangeState(dummy.DummyAlertState);
			}
			timer.Tick();
		}
		else if (base.Creature.IsPlayerInSightBound())
		{
			List<AttackSequence> attackSequences = dummy.GetAttackSequences();
			if (attackSequences == null)
			{
				base.Creature.ResetChance();
			}
			else
			{
				ChangeState(new ChaseState(dummy, attackSequences));
			}
		}
		else
		{
			ChangeState(null);
		}
	}
}
