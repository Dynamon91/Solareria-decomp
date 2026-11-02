using System;
using System.Collections.Generic;
using Library;

namespace SPR.FSM.Base;

public class BossAttackState : BossState
{
	private readonly Func<List<AttackSequence>> createAttackSequence;

	private List<AttackSequence> currentSequences;

	private int sequenceIndex;

	public AttackSequence CurrentSequence { get; private set; }

	public AttackSequence NextSequence { get; private set; }

	public AttackSequence LastSequence { get; private set; }

	public float LastSequenceFatigue { get; private set; }

	public BossAttackState(BossCharacter boss, Func<List<AttackSequence>> createAttackSequence)
		: base(boss)
	{
		this.createAttackSequence = createAttackSequence;
	}

	public override void Enter(IState prevState)
	{
		currentSequences = createAttackSequence();
		currentSequences.ForEach(delegate(AttackSequence seq)
		{
			seq.ResetFrame(base.Boss);
		});
		sequenceIndex = 0;
		CurrentSequence = currentSequences[sequenceIndex];
		NextSequence = ((sequenceIndex + 1 < currentSequences.Count) ? currentSequences[sequenceIndex + 1] : null);
	}

	public override void Exit(IState nextState)
	{
		if (CurrentSequence != null)
		{
			float fatigue = CurrentSequence.CastObject<BossAttackSequence>().Fatigue;
			if (fatigue > 0f)
			{
				LastSequenceFatigue = fatigue;
				LastSequence = CurrentSequence;
			}
		}
		base.Boss.OnAttackEnd(nextState);
	}

	public override void FixedUpdate(float fdt)
	{
		if (currentSequences.Count == 0 || sequenceIndex >= currentSequences.Count)
		{
			ChangeState(base.Boss.IdleState);
			return;
		}
		if (CurrentSequence.IsFirstFrame)
		{
			CurrentSequence.Start(base.Enemy);
		}
		if (CurrentSequence.IsDone)
		{
			ShiftNextSequence();
		}
		else
		{
			CurrentSequence.FixedUpdate(base.Boss, fdt);
		}
	}

	public void AddAttackSequence(List<BossAttackSequence> attackSequences)
	{
		int num = 0;
		foreach (BossAttackSequence attackSequence in attackSequences)
		{
			if (sequenceIndex + num + 1 == currentSequences.Count)
			{
				currentSequences.Add(attackSequence);
				num++;
			}
			else
			{
				currentSequences.Insert(sequenceIndex + 1, attackSequence);
			}
		}
	}

	public void ShiftNextSequence()
	{
		sequenceIndex++;
		if (sequenceIndex >= currentSequences.Count)
		{
			ChangeState(base.Boss.IdleState);
			return;
		}
		CurrentSequence.ResetFrame(base.Boss);
		CurrentSequence = currentSequences[sequenceIndex];
		NextSequence = ((sequenceIndex + 1 < currentSequences.Count) ? currentSequences[sequenceIndex + 1] : null);
		CurrentSequence.ResetFrame(base.Boss);
	}

	public BossAttackSequence GetFirstSequence()
	{
		return currentSequences[0].CastObject<BossAttackSequence>();
	}
}
