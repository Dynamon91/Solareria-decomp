using System;
using System.Collections.Generic;
using Library;
using SPR.FSM;

namespace SPR;

public class ImahenTurretAttackState : EnemyState
{
	private readonly ImahenTurretBase turretBase;

	private readonly Func<List<AttackSequence>> createAttackSequence;

	private List<AttackSequence> currentSequences;

	private int sequenceIndex;

	public AttackSequence CurrentSequence { get; private set; }

	public ImahenTurretAttackState(Enemy enemy, Func<List<AttackSequence>> createAttackSequence)
		: base(enemy)
	{
		turretBase = enemy.CastObject<ImahenTurretBase>();
		this.createAttackSequence = createAttackSequence;
	}

	public override void Enter(IState prevState)
	{
		currentSequences = createAttackSequence();
		currentSequences.ForEach(delegate(AttackSequence seq)
		{
			seq.ResetFrame(turretBase);
		});
		sequenceIndex = 0;
		CurrentSequence = currentSequences[sequenceIndex];
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (CurrentSequence.IsFirstFrame)
		{
			CurrentSequence.Start(base.Enemy);
		}
		if (CurrentSequence.IsDone)
		{
			sequenceIndex++;
			CurrentSequence.ResetFrame(turretBase);
			if (sequenceIndex >= currentSequences.Count)
			{
				ChangeState(turretBase.IdleState);
				return;
			}
			CurrentSequence = currentSequences[sequenceIndex];
		}
		CurrentSequence.FixedUpdate(turretBase, fdt);
	}
}
