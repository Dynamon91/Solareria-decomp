using System;
using System.Collections.Generic;
using Library;

namespace SPR.FSM.Base;

public class AttackState : CreatureState
{
	private readonly Func<List<AttackSequence>> createAttackSequence;

	private List<AttackSequence> currentSequences;

	private int sequenceIndex;

	private bool forceAttack;

	public AttackSequence CurrentSequence { get; private set; }

	public AttackState(Creature enemy, Func<List<AttackSequence>> createAttackSequence, bool forceAttack = false)
		: base(enemy)
	{
		this.createAttackSequence = createAttackSequence;
		this.forceAttack = forceAttack;
	}

	public override void Enter(IState prevState)
	{
		if (!forceAttack && prevState != base.Creature.AttackWaitState && CryptoRandom.RandomBool())
		{
			ChangeState(base.Creature.AttackWaitState);
			return;
		}
		currentSequences = createAttackSequence();
		currentSequences.ForEach(delegate(AttackSequence seq)
		{
			seq.ResetFrame(base.Creature);
		});
		sequenceIndex = 0;
		CurrentSequence = currentSequences[sequenceIndex];
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (currentSequences == null || currentSequences.Count == 0)
		{
			ChangeState(base.Creature.AlertState);
			return;
		}
		if (CurrentSequence.IsFirstFrame)
		{
			CurrentSequence.Start(base.Creature);
		}
		if (CurrentSequence.IsDone)
		{
			sequenceIndex++;
			if (sequenceIndex >= currentSequences.Count)
			{
				ChangeState(base.Creature.AlertState);
				return;
			}
			CurrentSequence.ResetFrame(base.Enemy);
			CurrentSequence = currentSequences[sequenceIndex];
		}
		else
		{
			CurrentSequence.FixedUpdate(base.Creature, fdt);
		}
	}
}
