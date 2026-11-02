using System.Collections.Generic;
using System.Linq;
using Library;

namespace SPR.FSM.Base;

public class BossIdleState : BossState
{
	protected BossAttackSequence Sequence;

	protected readonly List<BossAttackSequence> SequenceList;

	public BossIdleState(BossCharacter boss)
		: base(boss)
	{
		SequenceList = new List<BossAttackSequence>
		{
			base.Boss.Idle,
			base.Boss.MoveForward,
			base.Boss.MoveBackward
		};
	}

	public override void Enter(IState prevState)
	{
		if (base.Boss.CanMove)
		{
			SelectPattern();
			Sequence.ResetFrame(base.Boss);
		}
		else if (base.Boss.SkeletonAnimation.AnimationState.Tracks.IsNullOrEmpty())
		{
			base.Boss.SetAnimation(base.Boss.IdleAnimation);
		}
		else
		{
			base.Boss.AddAnimation(base.Boss.IdleAnimation);
		}
	}

	public override void Exit(IState nextState)
	{
	}

	public override void FixedUpdate(float fdt)
	{
		if (base.Boss.TryPhaseTransition())
		{
			return;
		}
		base.Boss.OnIdle();
		if (base.Boss.CanMove)
		{
			if (Sequence.IsFirstFrame)
			{
				Sequence.Start(base.Enemy);
			}
			Sequence.FixedUpdate(base.Boss, fdt);
		}
		base.Boss.AddFatigue(0f - fdt);
		if (base.Boss.Fatigue == 0f)
		{
			ChangeState(base.Boss.AttackState);
		}
	}

	private void SelectPattern()
	{
		List<int> list = new List<int>();
		list.AddRange(Enumerable.Repeat(0, base.Boss.Idle.GetPower(base.Boss)));
		list.AddRange(Enumerable.Repeat(1, base.Boss.MoveForward.GetPower(base.Boss)));
		list.AddRange(Enumerable.Repeat(2, base.Boss.MoveBackward.GetPower(base.Boss)));
		Sequence = SequenceList[list[CryptoRandom.Range(0, list.Count)]];
	}
}
