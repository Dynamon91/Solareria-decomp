using System;
using System.Collections.Generic;
using Library;
using SPR.FSM;
using SPR.ObjectPool;

namespace SPR;

public class TycheCloneAttackState : EnemyState
{
	private readonly Func<List<AttackSequence>> createAttackSequences;

	private List<AttackSequence> attackSequences;

	private int sequenceIndex;

	private readonly TycheClone tycheClone;

	private AttackSequence sequence;

	public TycheCloneAttackState(TycheClone clone, Func<List<AttackSequence>> attackSequences)
		: base(clone)
	{
		tycheClone = clone;
		createAttackSequences = attackSequences;
	}

	public override void Enter(IState prevState)
	{
		attackSequences = createAttackSequences();
		attackSequences.ForEach(delegate(AttackSequence seq)
		{
			seq.ResetFrame(tycheClone);
		});
		sequenceIndex = 0;
		sequence = attackSequences[sequenceIndex];
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (sequence.IsFirstFrame)
		{
			sequence.Start(tycheClone);
		}
		if (sequence.IsDone)
		{
			sequenceIndex++;
			if (sequenceIndex >= attackSequences.Count)
			{
				tycheClone.CastObject<IPoolableObject>().ReturnToPool();
				return;
			}
			sequence = attackSequences[sequenceIndex];
		}
		sequence.FixedUpdate(tycheClone, fdt);
	}
}
