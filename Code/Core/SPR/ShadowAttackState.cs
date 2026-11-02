using System;
using System.Collections.Generic;
using Library;
using SPR.FSM;
using SPR.ObjectPool;

namespace SPR;

public class ShadowAttackState : EnemyState
{
	private readonly Func<List<AttackSequence>> createAttackSequence;

	private List<AttackSequence> currentSequences;

	private int sequenceIndex;

	private readonly Enemy shadow;

	private AttackSequence sequence;

	public ShadowAttackState(Enemy enemy, Func<List<AttackSequence>> createAttackSequence)
		: base(enemy)
	{
		shadow = enemy;
		this.createAttackSequence = createAttackSequence;
	}

	public override void Enter(IState prevState)
	{
		currentSequences = createAttackSequence();
		currentSequences.ForEach(delegate(AttackSequence seq)
		{
			seq.ResetFrame(shadow);
		});
		sequenceIndex = 0;
		sequence = currentSequences[sequenceIndex];
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (sequence.IsFirstFrame)
		{
			sequence.Start(shadow);
		}
		if (sequence.IsDone)
		{
			sequenceIndex++;
			if (sequenceIndex >= currentSequences.Count)
			{
				shadow.CastObject<IPoolableObject>().ReturnToPool();
			}
			else
			{
				sequence = currentSequences[sequenceIndex];
			}
		}
		else
		{
			sequence.FixedUpdate(shadow, fdt);
		}
	}
}
